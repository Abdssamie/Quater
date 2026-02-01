# Docker Compose Setup

This directory contains the Docker Compose configuration for the Quater application.

## Security Improvements

The docker-compose.yml has been refactored with the following security enhancements:

### ✅ Implemented Fixes

1. **Environment Variables**: All sensitive credentials moved to `.env` file
2. **Redis Authentication**: Password protection enabled for Redis
3. **Removed Exposed Ports**: PostgreSQL and Redis ports no longer exposed to host (internal network only)
4. **Fixed Connection String**: Corrected `IncludeErrorDetail` syntax
5. **Pinned Versions**: PgAdmin now uses specific version (8.2) instead of `latest`
6. **Resource Limits**: Added CPU and memory limits for all services
7. **Logging Configuration**: Configured log rotation (10MB max, 3 files)
8. **Removed Deprecated Version**: Removed `version: '3.8'` (not needed in Compose v2)

## Getting Started

### 1. Configure Environment Variables

Copy the example environment file and update with your secure passwords:

```bash
cp .env.example .env
```

**Important**: Edit `.env` and replace all placeholder passwords with strong, unique passwords:

```env
POSTGRES_PASSWORD=<your-secure-password>
REDIS_PASSWORD=<your-secure-password>
PGADMIN_PASSWORD=<your-secure-password>
```

### 2. Start Services

```bash
docker-compose up -d
```

### 3. Verify Services

Check that all services are healthy:

```bash
docker-compose ps
```

### 4. View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f backend
```

## Service Details

### Backend (ASP.NET Core)
- **Ports**: 8080 (HTTP), 8443 (HTTPS)
- **Resources**: 2 CPU cores, 2GB RAM max
- **Health Check**: `/api/health/live` endpoint

### PostgreSQL
- **Internal Only**: No external port exposure
- **Resources**: 1 CPU core, 1GB RAM max
- **Data**: Persisted in `postgres_data` volume

### Redis
- **Internal Only**: No external port exposure
- **Authentication**: Password required (from `.env`)
- **Resources**: 0.5 CPU cores, 512MB RAM max
- **Persistence**: AOF (Append Only File) enabled

### PgAdmin
- **Port**: 5050
- **Access**: http://localhost:5050
- **Resources**: 0.5 CPU cores, 512MB RAM max

## Connecting to Databases

### From Host Machine

Since PostgreSQL and Redis ports are no longer exposed, use one of these methods:

**Option 1: Use PgAdmin** (Recommended)
- Access PgAdmin at http://localhost:5050
- Add server with hostname: `postgres`

**Option 2: Exec into Container**
```bash
# PostgreSQL
docker exec -it quater-postgres psql -U quater_user -d quater_db

# Redis
docker exec -it quater-redis redis-cli -a <your-redis-password>
```

**Option 3: Temporarily Expose Ports** (Development Only)
Add to docker-compose.yml if needed:
```yaml
postgres:
  ports:
    - "5432:5432"
redis:
  ports:
    - "6379:6379"
```

### From Application Code

Services communicate via internal Docker network:
- PostgreSQL: `postgres:5432`
- Redis: `redis:6379`

## Maintenance

### Stop Services
```bash
docker-compose down
```

### Stop and Remove Volumes (⚠️ Deletes Data)
```bash
docker-compose down -v
```

### Rebuild Services
```bash
docker-compose up -d --build
```

### View Resource Usage
```bash
docker stats
```

## Security Notes

- ⚠️ The `.env` file is excluded from git via `.gitignore`
- ⚠️ Never commit `.env` file to version control
- ⚠️ Use strong, unique passwords for production
- ⚠️ For production, consider using Docker secrets or external secret management
- ⚠️ Review and adjust resource limits based on your infrastructure

## Troubleshooting

### Service Won't Start
```bash
# Check logs
docker-compose logs <service-name>

# Restart specific service
docker-compose restart <service-name>
```

### Database Connection Issues
```bash
# Verify PostgreSQL is healthy
docker-compose ps postgres

# Check PostgreSQL logs
docker-compose logs postgres
```

### Redis Connection Issues
```bash
# Test Redis connection with password
docker exec -it quater-redis redis-cli -a <your-redis-password> ping
# Should return: PONG
```
