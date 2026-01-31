# Quater Deployment Infrastructure

This directory contains deployment configurations and scripts for the Quater water quality lab management system.

## 📁 Directory Structure

```
.
├── .github/workflows/       # GitHub Actions CI/CD pipelines
│   ├── backend-ci.yml      # Continuous Integration for backend
│   ├── backend-cd.yml      # Continuous Deployment for backend
│   └── docker-compose-test.yml  # Docker Compose integration tests
├── docker/                  # Docker configurations
│   ├── docker-compose.yml  # Local development environment
│   └── init.sql            # Database initialization script
├── Dockerfile              # Multi-stage Docker build
├── .dockerignore           # Docker build context exclusions
├── deploy.sh               # Deployment automation script
└── .env.example            # Environment variables template

```

## 🚀 Quick Start

### Local Development with Docker Compose

1. **Start all services:**
   ```bash
   cd docker
   docker compose up -d
   ```

2. **Access the services:**
   - Backend API: http://localhost:8080
   - Swagger UI: http://localhost:8080/swagger
   - pgAdmin: http://localhost:5050
   - PostgreSQL: localhost:5432
   - Redis: localhost:6379

3. **View logs:**
   ```bash
   docker compose logs -f backend
   ```

4. **Stop services:**
   ```bash
   docker compose down
   ```

### Manual Deployment

Use the deployment script for different environments:

```bash
# Development (default)
./deploy.sh dev

# Staging
./deploy.sh staging

# Production
./deploy.sh production

# With options
./deploy.sh dev --skip-tests
./deploy.sh staging --no-push
```

## 🔧 Configuration

### Environment Variables

Copy `.env.example` to `.env` and update the values:

```bash
cp .env.example .env
```

Key configuration areas:
- **Database**: PostgreSQL connection settings
- **Redis**: Cache and rate limiting
- **Authentication**: JWT and OpenIddict secrets
- **CORS**: Allowed origins for clients
- **Logging**: Log levels and paths

### Docker Configuration

The `Dockerfile` uses multi-stage builds:
1. **build**: Restore dependencies and compile
2. **test**: Run unit tests
3. **publish**: Create optimized artifacts
4. **runtime**: Minimal production image

### Docker Compose Services

- **backend**: ASP.NET Core API (port 8080)
- **postgres**: PostgreSQL 15 database (port 5432)
- **redis**: Redis cache (port 6379)
- **pgadmin**: Database management UI (port 5050)

## 🔄 CI/CD Pipelines

### Backend CI (`backend-ci.yml`)

Runs on every push and pull request to `main` or `develop`:

- ✅ Build .NET application
- ✅ Run unit tests with code coverage
- ✅ Lint and format check
- ✅ Security vulnerability scan
- ✅ Upload test results and coverage reports

### Backend CD (`backend-cd.yml`)

Runs on push to `main` or manual trigger:

- 🐳 Build Docker image
- 📦 Push to GitHub Container Registry
- 🚀 Deploy to staging environment
- 🧪 Run smoke tests
- 🎯 Deploy to production (manual approval)

### Docker Compose Test (`docker-compose-test.yml`)

Tests the complete Docker Compose setup:

- 🔧 Start all services
- ❤️ Verify health checks
- 🌐 Test API endpoints
- 🗄️ Verify database connectivity

## 📊 Health Checks

The backend provides multiple health check endpoints:

- `/api/health/live` - Liveness probe (is the app running?)
- `/api/health/ready` - Readiness probe (can it accept traffic?)
- `/api/health/startup` - Startup probe (has it finished starting?)
- `/api/health` - Detailed health status

## 🔐 Security

### Production Checklist

- [ ] Change all default passwords
- [ ] Generate secure JWT signing keys (min 32 characters)
- [ ] Configure SSL/TLS certificates
- [ ] Set up proper CORS origins
- [ ] Enable email confirmation for user accounts
- [ ] Configure production logging (remove sensitive data)
- [ ] Set up backup retention policies
- [ ] Review rate limiting settings
- [ ] Enable database SSL connections
- [ ] Configure firewall rules

### Secrets Management

**Never commit secrets to version control!**

For production:
- Use environment variables
- Use secret management services (Azure Key Vault, AWS Secrets Manager, etc.)
- Use GitHub Secrets for CI/CD
- Rotate secrets regularly

## 🐛 Troubleshooting

### Container won't start

```bash
# Check logs
docker compose logs backend

# Check health status
docker compose ps

# Restart specific service
docker compose restart backend
```

### Database connection issues

```bash
# Verify postgres is running
docker compose ps postgres

# Test connection
docker compose exec postgres psql -U quater_user -d quater_db -c "SELECT version();"
```

### Build failures

```bash
# Clean build
docker compose build --no-cache backend

# Check Dockerfile syntax
docker build -t test -f Dockerfile .
```

## 📈 Monitoring

### Logs

Logs are written to:
- Console (stdout/stderr) - captured by Docker
- File system: `/app/logs` (mounted volume)
- Database: `logs` table (warnings and errors only)

### Metrics

Consider adding:
- Application Insights / Prometheus
- Health check monitoring
- Performance counters
- Custom business metrics

## 🔄 Updates and Maintenance

### Updating Dependencies

```bash
# Update NuGet packages
cd backend
dotnet outdated
dotnet add package <PackageName>

# Update Docker base images
docker pull mcr.microsoft.com/dotnet/sdk:8.0-alpine
docker pull mcr.microsoft.com/dotnet/aspnet:8.0-alpine
```

### Database Migrations

```bash
# Create migration
cd backend/src/Quater.Backend.Api
dotnet ef migrations add MigrationName

# Apply migrations (automatic on startup)
# Or manually:
dotnet ef database update
```

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Docker Documentation](https://docs.docker.com)
- [GitHub Actions Documentation](https://docs.github.com/actions)
- [PostgreSQL Documentation](https://www.postgresql.org/docs)
- [Redis Documentation](https://redis.io/documentation)

## 🤝 Contributing

When adding new deployment features:

1. Update this README
2. Test locally with Docker Compose
3. Verify CI/CD pipelines pass
4. Document any new environment variables
5. Update `.env.example` if needed

## 📝 License

See the main project LICENSE file.
