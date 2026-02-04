# Multi-stage Dockerfile for Quater Backend API
# Build arguments for versioning
ARG VERSION=0.0.0-dev
ARG BUILD_DATE=unknown

# Stage 1: Build - Restore dependencies and build the application
# Use .NET 9 SDK to avoid .NET 10 MSBuild wildcard expansion bug in Docker
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /App

# Copy solution file
COPY Quater.sln ./

# Copy all source code
COPY shared/ ./shared/
COPY backend/ ./backend/

# Restore dependencies (cached layer if project files unchanged)
RUN dotnet restore backend/Quater.Backend.sln

# Build the application in Release mode
WORKDIR /App/backend/src/Quater.Backend.Api
RUN dotnet build -c Release --no-restore

# Stage 2: Test - Run unit tests
FROM build AS test
WORKDIR /App/backend/tests
RUN dotnet test -c Release --no-build --no-restore --logger "trx;LogFileName=test_results.trx" || true

# Stage 3: Publish - Create optimized runtime artifacts
FROM build AS publish
WORKDIR /App/backend/src/Quater.Backend.Api
RUN dotnet publish -c Release -o /app/publish --no-restore --no-build

# Stage 4: Runtime - Minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Bring build args into runtime stage
ARG VERSION
ARG BUILD_DATE

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Copy published artifacts from publish stage
COPY --from=publish --chown=ubuntu:ubuntu /app/publish .

# Switch to non-root user
USER ubuntu

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    APP_VERSION=${VERSION} \
    BUILD_DATE=${BUILD_DATE}

# Add image labels for metadata
LABEL version="${VERSION}" \
      build-date="${BUILD_DATE}" \
      maintainer="Quater Team"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Quater.Backend.Api.dll"]
