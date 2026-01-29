# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY backend/Quater.Backend.sln ./
COPY backend/src/Quater.Backend.Api/Quater.Backend.Api.csproj backend/src/Quater.Backend.Api/
COPY backend/src/Quater.Backend.Core/Quater.Backend.Core.csproj backend/src/Quater.Backend.Core/
COPY backend/src/Quater.Backend.Data/Quater.Backend.Data.csproj backend/src/Quater.Backend.Data/
COPY backend/src/Quater.Backend.Sync/Quater.Backend.Sync.csproj backend/src/Quater.Backend.Sync/
COPY backend/tests/Quater.Backend.Core.Tests/Quater.Backend.Core.Tests.csproj backend/tests/Quater.Backend.Core.Tests/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY backend/ ./backend/

# Build and publish
WORKDIR /src/backend/src/Quater.Backend.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8443

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Entry point
ENTRYPOINT ["dotnet", "Quater.Backend.Api.dll"]
