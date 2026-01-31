# GitHub Actions CI/CD Pipelines

This directory contains GitHub Actions workflows for continuous integration and deployment of the Quater backend.

## 📋 Workflows

### 1. Backend CI (`backend-ci.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Changes to backend code, shared code, or Dockerfile

**Jobs:**

#### Build and Test
- Sets up .NET 8.0
- Starts PostgreSQL and Redis test services
- Restores dependencies with caching
- Builds the application
- Runs unit tests with code coverage
- Uploads test results and coverage reports
- Posts coverage summary to PRs

#### Lint and Format
- Checks code formatting with `dotnet format`
- Ensures code style consistency

#### Security Scan
- Scans for vulnerable NuGet packages
- Reports security issues

**Required Secrets:** None (uses GitHub token automatically)

---

### 2. Backend CD (`backend-cd.yml`)

**Triggers:**
- Push to `main` branch
- Manual workflow dispatch with environment selection

**Jobs:**

#### Build and Push
- Builds Docker image using multi-stage Dockerfile
- Pushes to GitHub Container Registry (ghcr.io)
- Tags with branch name, SHA, and `latest`
- Uses Docker layer caching for faster builds
- Generates build attestation for security

#### Deploy to Staging
- Automatically deploys on push to `main`
- Runs smoke tests
- Verifies deployment health

#### Deploy to Production
- Requires manual approval
- Only runs when explicitly triggered
- Runs smoke tests
- Sends deployment notifications

**Required Secrets:**
- `GITHUB_TOKEN` (automatically provided)
- Add deployment-specific secrets as needed (SSH keys, cloud credentials, etc.)

**Environment Configuration:**
- Configure `staging` and `production` environments in GitHub repository settings
- Add environment protection rules and required reviewers

---

### 3. Docker Compose Test (`docker-compose-test.yml`)

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Changes to Docker configuration files

**Jobs:**

#### Test Docker Compose Setup
- Starts all services with Docker Compose
- Verifies service health
- Tests backend API endpoints
- Checks database connectivity
- Shows logs on failure

**Purpose:** Ensures the Docker Compose setup works correctly for local development.

---

## 🚀 Setup Inn
### 1. Enable GitHub Actions

GitHub Actions should be enabled by default. Verify in:
- Repository Settings → Actions → General → Allow all actions

### 2. Configure Environments

Create environments for deployment:

1. Go to: Repository Settings → Environments
2. Create `staging` environment:
   - Add environment URL: `https://staging.quater.app`
   - Optional: Add required reviewers
3. Create `production` environment:
   - Add environment URL: `https://quater.app`
   - **Required:** Add required reviewers for approval
   - **Required:** Add deployment branch rule (only `m`)

### 3. Configure Secrets

Add secrets in: Repository Settings → Secrets and variables → Actions

**Repository Secrets:**
```
# Docker Registry (if using external registry)
DOCKER_USERNAME=your-username
DOCKER_PASSWORD=your-password

# Cloud Provider Credentials (if deploying to cloud)
AZURE_CREDENTIALS=<azure-service-principal-json>
AWS_ACCESS_KEY_ID=<your-key>
AWS_SECRET_ACCESS_KEY=<your-secret>
```

**Environment Secrets (per environment):**
```
# Staging
DB_CONNECTION_STRING=<staging-db-connection>
REDIS_CONNECTION_STRING=<staging-redis-connection>
JWT_SECRET_KEY=<staging-jwt-secret>

# Production
DB_CONNECTION_STRING=<production-db-connection>
REDIS_CONNECTION_STRING=<production-redis-connection>
JWT_SECRET_KEY=<production-jwt-secret>
```

### 4. Configure Container Registry

The workflows use GitHub Container Registry (ghcr.io) by default.

**Permissions:**
- Workflows have `packages: write` permission
- Images are automatically pushed to `ghcr.io/<owner>/<repo>/backend`

**To use Docker Hub instead:**
1. Add `DOCKER_USERNAME` and `DOCKER_PASSWORD` secrets
2. Update `backend-cd.yml`:
   ```yaml
   env:
     REGISTRY: docker.io
     IMAGE_NAME: your-username/quater-backend
   ```

---

## 📊 Monitoring Workflows

### View Workflow Runs

- Go to: Repository → Actions tab
- Click on a workflow to see runs
- Click on a run to see job details and logs

### Workflow Status Badges

Add to your README.md:

```markdown
![Backend CI](https://github.com/<owner>/<repo>/actions/workflows/backend-ci.yml/badge.svg)
![Backend CD](https://github.com/<owner>/<repo>/actions/workflows/backend-cd.yml/badge.svg)
```

### Notifications

Configure notifications in: GitHub Settings → Notifications → Actions

---

## 🔧 Customization

### Modify Deployment Logic

Edit the deployment steps in `backend-cd.yml`:

**Example: Dloy to Azure Web App**
```yaml
- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
```

**Example: Deploy to Kubernetes**
```yaml
- name: Deploy to Kubernetes
  run: |
    kubectl set image deployment/quater-backend \
      quater-backend=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
    kubectl rollout status deployment/quater-backend
```

**Example: Deploy via SSH**
```yaml
- name: Deploy to Server
  uses: appleboy/ssh-action@master
  with:
    host: ${{ secrets.SERVER_HOST }}
    username: ${{ secrets.SERVER_USER }}
    key: ${{ secrets.SSH_PRIVATE_KEY }}
    script: |
      cd /app/quater
      docker pull ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}
      docker-compose up -d
```

### Add Additional Tests

Add integration tests or E2E tests:

```yaml
- name: Run integration tests
  run: |
    dotnet test backend/tests/Quater.Backend.IntegrationTests \
      --configuration Release \
      --logger "trx;leName=integration-test-results.trx"
```

### Add Code Quality Checks

Add SonarCloud, CodeQL, or other analysis tools:

```yaml
- name: SonarCloud Scan
  uses: SonarSource/sonarcloud-github-action@master
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

---

## 🐛 Troubleshooting

### Workflow Fails on Test Step

**Check:**
- Test service containers (PostgreSQL, Redis) are healthy
- Connection strings are correct
- Tests pass locally

**Debug:**
```yaml
- name: Debug test failure
  if: failure()
  run: |
    docker ps
    docker logs <container-id>
```

### Docker Build Fails

**Check:**
- Dockerfile syntax is correct
- All required files are not in `.dockerignore`
- Base images are accessible

**Debug:**
```yaml
- name: Build with verbose output
  run: docker build --progress=plain -t test .
```

### Deployment Fails

**Check:**
- Secrets are configured correctly
- Target environment is accessible
- Image was pushed successfully

**Debug:**
```yaml
- name: Show deployment info
  run: |
    echo "Image: ${{ needs.build-and-push.outputs.image-tag }}"
    ech${{ needs.build-and-push.outputs.image-digest }}"
```

### Permission Denied Errors

**Check:**
- Workflow has correct permissions
- `GITHUB_TOKEN` has package write access
- Repository settings allow Actions to create packages

**Fix:**
```yaml
permissions:
  contents: read
  packages: write
```

---

## 📚 Best Practices

### 1. Use Caching

Cache dependencies to speed up builds:
```yaml
- uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/packages.lock.json') }}
```

### 2. Use Matrix Builds

Test on multiple p``yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
    dotnet: ['8.0.x']
```

### 3. Fail Fast

Stop on first failure:
```yaml
strategy:
  fail-fast: true
```

### 4. Use Concurrency Control

Prevent concurrent deployments:
```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

### 5. Add Manual Approval

Require approval for production:
```yaml
environment:
  name: production
  # Configure required reviewers in GitHub UI
```

---

## 🔐 Security

### Secrets Management

- Never commit secrets to code
- Use GitHub Secrets for sensitive data
- Rotate secrets regularnvironment-specific secrets

### Image Security

- Scan images for vulnerabilities
- Use minimal base images (Alpine)
- Run as non-root user
- Keep dependencies updated

### Access Control

- Limit who can trigger workflows
- Require reviews for production deployments
- Use branch protection rules
- Enable signed commits

---

## 📖 Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Build Push Action](https://github.com/docker/build-push-action)
- [.NET Actions](https://github.com/actions/setup-dotnet)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)

---

## 🤝 Contributing

When modifying workflows:

1. Test changes in a feature branch
2. Verify workflows pass before merging
3. Update this documentation
4. Consider backward compatibility
5. Add comments for complex logic
