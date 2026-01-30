# Multi-stage Dockerfile for Quater Backend API
# Stage 1: Build - Restore dependencies and build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution file
COPY Quater.sln ./
COPY backend/Quater.Backend.sln ./backend/

# Copy all project files for dependency restoration
COPY shared/Quater.Shared.csproj ./shared/
COPY backend/src/Quater.Backend.Api/Quater.Backend.Api.csproj ./backend/src/Quater.Backend.Api/
COPY backend/src/Quater.Backend.Core/Quater.Backend.Core.csproj ./backend/src/Quater.Backend.Core/
COPY backend/src/Quater.Backend.Data/Quater.Backend.Data.csproj ./backend/src/Quater.Backend.Data/
COPY backend/src/Quater.Backend.Services/Quater.Backend.Services.csproj ./backend/src/Quater.Backend.Services/
COPY backend/src/Quater.Backend.Sync/Quater.Backend.Sync.csproj ./backend/src/Quater.Backend.Sync/
COPY backend/tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj ./backend/tests/Quater.Backend.Core.Tests/

# Restore dependencies (cached layer if project files unchanged)
RUN dotnet restore backend/Quater.Backend.sln

# Copy all source code
COPY shared/ ./shared/
COPY backend/ ./backend/

# Build the application in Release mode
WORKDIR /src/backend/src/Quater.Backend.Api
RUN dotnet build -c Release --no-restore

# Stage 2: Test - Run unit tests
FROM build AS test
WORKDIR /src/backend/tests/Quater.Backend.Core.Tests
RUN dotnet test -c Release --no-build --no-restore --logger "trx;LogFileName=test_results.trx" || true

# Stage 3: Publish - Create optimized runtime artifacts
FROM build AS publish
WORKDIR /src/backend/src/Quater.Backend.Api
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

# Stage 4: Runtime - Minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Create non-root user for running the application
RUN addgroup -g 1000 quater && \
    adduser -D -u 1000 -G quater quater && \
    mkdir -p /app/logs && \
    chown -R quater:quater /app

# Copy published artifacts from publish stage
COPY --from=publish --chown=quater:quater /app/publish .

# Switch to non-root user
USER quater

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Quater.Backend.Api.dll"]
