# Quater Backend API

Enterprise-grade water quality management system backend built with .NET 10, PostgreSQL, and OpenIddict OAuth2/OIDC.

## 🚀 Quick Start (Development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/get-started) (for PostgreSQL and Redis)
- [PostgreSQL 17](https://www.postgresql.org/download/) (or use Docker)
- [Redis](https://redis.io/download) (or use Docker)

### 1. Start Infrastructure Services

```bash
# Start PostgreSQL and Redis using Docker Compose
cd docker
docker-compose up -d postgres redis

# Verify services are running
docker-compose ps
```

### 2. Configure Development Settings

The `appsettings.Development.json` is pre-configured for local development:

- **Database**: `localhost:5434` (PostgreSQL in Docker)
- **Redis**: `localhost:6379`
- **OpenIddict Issuer**: `http://localhost:5000`
- **Email**: MailHog on `localhost:1025` (optional, for testing emails)

**No additional configuration needed for development!**

### 3. Run Database Migrations

```bash
cd backend/src/Quater.Backend.Api
dotnet ef database update
```

### 4. Run the API

```bash
cd backend/src/Quater.Backend.Api
dotnet run
```

The API will start on:
- **HTTP**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

### 5. Test the API

```bash
# Health check
curl http://localhost:5000/health/liveness

# Register a test user
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#Strong",
    "userName": "testuser",
    "role": "Technician",
    "labId": "00000000-0000-0000-0000-000000000001"
  }'
```

---

## 📦 Project Structure

```
backend/
├── src/
│   ├── Quater.Backend.Api/              # Web API (Controllers, Middleware)
│   ├── Quater.Backend.Core/             # Business logic, DTOs, Interfaces
│   ├── Quater.Backend.Data/             # EF Core, DbContext, Migrations
│   ├── Quater.Backend.Services/         # Service implementations
│   └── Quater.Backend.Infrastructure.Email/  # Email queue & templates
├── tests/
│   ├── Quater.Backend.Api.Tests/        # API integration tests
│   └── Quater.Backend.Core.Tests/       # Unit & integration tests
├── scripts/
│   └── generate-openiddict-certs.sh     # Certificate generation script
└── docs/
    └── INFISICAL_SETUP.md               # Secrets management guide
```

---

## 🔐 Authentication & Authorization

### OpenIddict OAuth2/OIDC

The API uses **OpenIddict** forAuthorization Code Flow with PKCE:

- **Issuer**: `http://localhost:5000` (dev) / `https://api.yourdomain.com` (prod)
- **Supported Flows**: Authorization Code + PKCE, Refresh Token
- **Token Security**: Signed + Encrypted (production requires certificates)
- **DPoP Support**: Optional (disabled in development)

### Development Mode (No Certificates Required)

In development, OpenIddict uses **ephemeral signing keys** (auto-generated on startup). This is **NOT secure for production** but convenient for local development.

### Production Mode (Certificates Required)

For production, you **must** generate X.509 certificates:

```bash
cd backend
./scripts/generate-openiddict-certs.sh production
```

See [AGENTS.md § OpenIddict Certificate Management](./AGENTS.md#-openiddict-certificate-management) for detailed instructions.

### User Roles

- **Admin**: Full system access (create/update/delete all resources)
- **Technician**: Create samples, test results, view reports
- **Viewer**: Read-only access to samples and reports

### Authorization Policies

```csharp
[Authorize(Policy = Policies.AdminOnly)]           // Admin only
[Authorize(Policy = Policies.TechnicianOrAbove)]   // Technici + Admin
[Authorize(Policy = Policies.ViewerOrAbove)]       // All authenticated users
```

---

## 🗄️ Database

### PostgreSQL Configuration

**Development** (Docker):
```
Host: localhost
Port: 5434
Database: quater_db
Username: quater_user
Password: quater_password
```

**Production**:
- Use environment variables or Infisical for connection strings
- Enable SSL/TLS: `SSL Mode=Require`
- Use strong passwords (min 16 characters)

### Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project src/Quater.Backend.Data

# Apply migrations
dotnet ef database update --project src/Quater.Backend.Api

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project src/Quater.Backend.Api

# Generate SQL script
dotnet ef migrations script --project src/Quater.Backend.Data --output migrations.sql
```

### Key Features

- **Soft Delete**: Entities are marked as deleted, not physically removed
- **Audit Trail**: Automatic tracking of Created/Updated timestamps and users
- **Optimistic Concurrency**: Row versioning prevents lost updates
- **Query Filters**: Soft-deleted entities automatically excluded from queries

---

## 📧 Email Configuration

### Development (MailHog)

MailHog is a fake SMTP server for testing emails locally:

```bash
# Start MailHog
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog

# View emails at: http://localhost:8025
```

Configuration in `appsettings.Development.json`:
```json
{
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "UseSsl": false,
    "FromAddress": "noreply@quater.local",
    "Enabled": true
  }
}
```

### Production (SendGrid, AWS SES, etc.)

Use environment variables:
```bash
EMAIL_SMTP_HOST=smtp.sendgrid.net
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=apikey
EMAIL_SMTP_PASSWORD=your-sendgrid-api-key
EMAIL_FROM_ADDRESS=noreply@yourdomain.com
EMAIL_USE_SSL=true
```

### Email Templates

Located in `src/Quater.Backend.Infrastructure.Email/Templates/`:
- `verification.html` - Email verification
- `password-reset.html` - Password reset
- `welcome.html` - Welcome email after verification
- `security-alert.html` - Security notifications

Templates use **Scriban** syntax for dynamic content.

---

## 🚦 Rate Limiting

### Configuration

**Development**:
- Authenticated users: 100 requests/minute
- Anonymous users: 20 requests/minute

**Production**:
- Authenticated users: 60 requests/minute
- Anonymous users: 10 requests/minute

### Redis Requirement

Rate limiting requires **Redis** for distributed state:

```bash
# Start Redis (Docker)
docker run -d -p 6379:6379 redis:7-alpine

# Or use Docker Compose
cd docker
docker-compose up -d redis
```

### Endpoint-Specific Limits

```csharp
[HttpPost("forgot-password")]
[EndpointRateLimit(requests: 3, windowMinutes: 60, trackBy: RateLimitTrackBy.Email)]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
```

---

## 🧪 Testing

### Run All Tests

```bash
# Run all tests
dotnet test backend/Quater.Backend.sln

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover

# Run specific test class
dotnet test --filter "FullyQualifiedName~SampleControllerTests"

# Run single test
dotnet test --filter "FullyQualifiedName=Quater.Backend.Api.Tests.SampleControllerTests.CreateSample_ValidData_ReturnsCreated"
```

### Test Infrastructure

- **TestContainers**: Spins up real PostgreSQL and Redis containers for integration tests
- **WebApplicationFactory**: In-memory API for controller tests
- **xUnit**: Test framework
- **FluentAssertions**: Readable assertions

### Test Coverage

- **167 tests** covering:
  - API controllers (authentication, authorization, CRUD)
  - Service layer (business logic)
  - Data layer (EF Core, interceptors)
  - Validators (FluentValidation)
  - Value objects and domain models

---

## 🏗️ Build & Deploy

### Build for Production

```bash
# Build Release configuration
dotnet build backend/Quater.Backend.sln --configuration Release

# Publish self-contained
dotnet publish backend/src/Quater.Backend.Api \
  --configuration Release \
  --output ./publish \
  --self-contained false
```

### Docker Deployment

```bash
# Build Docker image
docker build -t quater-backend:latest -f docker/Dockerfile.backend .

# Run with Docker Compose
cd docker
docker-compose up -d
```

### Environment Variables (Production)

**Required**:
```bash
ASPNETCORE_ENVIRONMENT=Production
DB_CONNECTION_STRING=Host=...;Database=...;Username=...;Password=...
OPENIDDICT_ISSUER=https://api.yourdomain.com
OPENIDDICT_AUDIENCE=quater-api
OPENIDDICT_ENCRYPTION_CERT_PATH=/app/certs/encryption.pfx
OPENIDDICT_SIGNING_CERT_PATH=/app/certs/signing.pfx
OPENIDDICT_ENCRYPTION_CERTSWORD=***
OPENIDDICT_SIGNING_CERT_PASSWORD=***
REDIS_CONNECTION_STRING=redis-server:6379,password=***
EMAIL_SMTP_HOST=smtp.sendgrid.net
EMAIL_SMTP_PASSWORD=***
```

**Optional**:
```bash
CORS_ORIGIN_1=https://app.yourdomain.com
RATELIMITING_AUTHENTICATED_LIMIT=60
RATELIMITING_ANONYMOUS_LIMIT=10
```

### Secrets Management

**Development**: Use `appsettings.Development.json` or User Secrets

```bash
dotnet user-secrets set "OpenIddict:ClientSecret" "your-secret"
```

**Production**: Use **Infisical**, Azure Key Vault, or AWS Secrets Manager

See [docs/INFISICAL_SETUP.md](./docs/INFISICAL_SETUP.md) for Infisical integration.

---

## 📊 Monitoring & Logging

### Serilog Configuration

Logs are written to:
- **Console**: Structured JSON logs
- **File**: `/app/logs/log-YYYYMMDD.txt` (rolling daily, 30-day retention)
- **PostgreSQL**: `logs` table (warnings and errors only)

### Health Checks

- **Liveness**: `GET /health/liveness` (always returns 200 if app is running)
- **Readiness**: `GET /health/readiness` (checks database, Redis, email)

### Metrics

Use Prometheus + Grafana for production monitoring:
- Request rate, latency, error rate
- Database on pool metrics
- Redis cache hit/miss ratio
- Rate limiting metrics

---

## 🔧 Troubleshooting

### "Configuration validation failed: OpenIddict:Issuer is not configured"

**Solution**: Update `appsettings.Development.json` with:
```json
{
  "OpenIddict": {
    "Issuer": "http://localhost:5000",
    "Audience": "quater-api"
  }
}
```

### "Unable to connect to PostgreSQL"

**Solution**: Ensure PostgreSQL is running:
```bash
docker-compose up -d postgres
docker-compose logs postgres
```

### "Redis connection failed"

**Solution**: Start Redis:
```bash
docker-compose up -d redis
```

### "Email sending failed"

**Solution**: 
- Development: Start MailHog (`docker run -p 1025:1025 -p 8025:8025 mailhog/mailhog`)
- Production: Verify SMTP credentials and firewall rules

### "Certificate not found" (Production)

**Solution**: Generate certificates:
```bash
cd backend
./scripts/generate-openiddict-certs.sh production
```

---

## 📚 Additional Documentation

- [AGENTS.md](./AGENTS.md) - AI agent instructions, coding standards, security best practices
- [docs/INFISICAL_SETUP.md](./docs/INFISICAL_SETUP.md) - Secrets management with Infisical
- [docs/DEPLOYMENT.md](../docs/DEPLOYMENT.md) - Production deployment guide
- [tests/TESTING_SUMMARY.md](./tests/TESTING_SUMMARY.md) - Test strategy and coverage
- [CERTIFICATES.md](../CERTIFICATES.md) - Certificate generation and rotation

---

## 🤝 Contributing

1. Follow the coding standards in [AGENTS.md](./AGENTS.md)
2. Write tests for all new features
3. Run `dotnet format` before committing
4. Ensure all tests pass: `dotnet test`
5. Update documentation as needed

---

## 📄 License

Proprietary - All rights reserved

---

## 🆘 Support

For issues or questions:
1. Check [AGENTS.md](./AGENTS.md) for coding guidelines
2. Review [Troubleshooting](#-troubleshooting) section
3. Check existing GitHub issues
4. Contact the development team
