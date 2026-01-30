#!/bin/bash

################################################################################
# Quater Backend API Deployment Script
# 
# This script automates the build, test, and deployment process for the
# Quater water quality lab management system backend API.
#
# Usage:
#   ./deploy.sh [environment] [options]
#
# Environments:
#   dev         - Development environment (default)
#   staging     - Staging environment
#   production  - Production environment
#
# Options:
#   --skip-tests    Skip running tests
#   --skip-build    Skip Docker build
#   --no-push       Don't push to registry
#   --help          Show this help message
################################################################################

set -e  # Exit on error
set -u  # Exit on undefined variable

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"
BACKEND_DIR="$PROJECT_ROOT/backend"
DOCKER_REGISTRY="${DOCKER_REGISTRY:-}"
IMAGE_NAME="${IMAGE_NAME:-quater-backend}"
VERSION="${VERSION:-latest}"

# Default options
ENVIRONMENT="${1:-dev}"
SKIP_TESTS=false
SKIP_BUILD=false
NO_PUSH=false

# Parse command line arguments
shift || true
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --no-push)
            NO_PUSH=true
            shift
            ;;
        --help)
            grep "^#" "$0" | grep -v "#!/bin/bash" | sed 's/^# //'
            exit 0
            ;;
        *)
            echo -e "${RED}Unknown option: $1${NC}"
            exit 1
            ;;
    esac
done

################################################################################
# Helper Functions
################################################################################

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check if Docker Compose is installed
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check if .NET SDK is installed (for local builds)
    if ! command -v dotnet &> /dev/null; then
        log_warning ".NET SDK is not installed. Skipping local build checks."
    fi
    
    log_success "Prerequisites check passed"
}

build_backend() {
    log_info "Building backend application..."
    
    cd "$BACKEND_DIR"
    
    # Restore dependencies
    log_info "Restoring NuGet packages..."
    dotnet restore Quater.Backend.sln
    
    # Build in Release mode
    log_info "Building in Release mode..."
    dotnet build Quater.Backend.sln -c Release --no-restore
    
    log_success "Backend build completed"
}

run_tests() {
    log_info "Running tests..."
    
    cd "$BACKEND_DIR"
    
    # Run all tests
    log_info "Executing unit tests..."
    dotnet test Quater.Backend.sln -c Release --no-build --verbosity normal --logger "trx;LogFileName=test_results.trx"
    
    TEST_EXIT_CODE=$?
    
    if [ $TEST_EXIT_CODE -ne 0 ]; then
        log_error "Tests failed with exit code $TEST_EXIT_CODE"
        exit $TEST_EXIT_CODE
    fi
    
    log_success "All tests passed"
}

build_docker_image() {
    log_info "Building Docker image..."
    
    cd "$PROJECT_ROOT"
    
    # Determine image tag
    if [ -n "$DOCKER_REGISTRY" ]; then
        IMAGE_TAG="$DOCKER_REGISTRY/$IMAGE_NAME:$VERSION"
    else
        IMAGE_TAG="$IMAGE_NAME:$VERSION"
    fi
    
    # Build Docker image
    log_info "Building image: $IMAGE_TAG"
    docker build -t "$IMAGE_TAG" -f Dockerfile .
    
    # Tag with environment
    docker tag "$IMAGE_TAG" "$IMAGE_NAME:$ENVIRONMENT"
    
    log_success "Docker image built: $IMAGE_TAG"
}

push_docker_image() {
    if [ -z "$DOCKER_REGISTRY" ]; then
        log_warning "DOCKER_REGISTRY not set. Skipping push."
        return
    fi
    
    log_info "Pushing Docker image to registry..."
    
    IMAGE_TAG="$DOCKER_REGISTRY/$IMAGE_NAME:$VERSION"
    
    # Push versioned tag
    docker push "$IMAGE_TAG"
    
    # Push environment tag
    docker tag "$IMAGE_TAG" "$DOCKER_REGISTRY/$IMAGE_NAME:$ENVIRONMENT"
    docker push "$DOCKER_REGISTRY/$IMAGE_NAME:$ENVIRONMENT"
    
    log_success "Docker image pushed to registry"
}

deploy_local() {
    log_info "Deploying to local environment..."
    
    cd "$PROJECT_ROOT"
    
    # Stop existing containers
    log_info "Stopping existing containers..."
    docker-compose -f docker/docker-compose.yml down || true
    
    # Start new containers
    log_info "Starting containers..."
    docker-compose -f docker/docker-compose.yml up -d
    	 for services to be healthy
    log_info "Waiting for services to be healthy..."
    sleep 10
    
    # Check service health
    if docker-compose -f docker/docker-compose.yml ps | grep -q "Up"; then
        log_success "Services are running"
        docker-compose -f docker/docker-compose.yml ps
    else
        log_error "Services failed to start"
        docker-compose -f docker/docker-compose.yml logs
        exit 1
    fi
    
    log_success "Local deployment completed"
}

deploy_remote() {
    log_info "Deploying to $ENVIRONMENT environment..."
    
    # This is a placeholder for remote deployment logic
    # In a real scenario, you would:
    # 1. SSH into the remote server
    # 2. Pull the latest Docker image
    # 3. Update the docker-compose.yml or Kubernetes manifests
    # 4. Restart the services
    
    log_warning "Remote deployment not implemented. Please deploy manually."
    log_info "Image to deploy: $DOCKER_REGISTRY/$IMAGE_NAME:$VERSION"
}

cleanup() {
    log_info "Cleaning up..."
    
    # Remove dangling images
    docker image prune -f
    
    log_success "Cleanup completed"
}

################################################################################
# Main Deployment Flow
################################################################################

main() {
    log_info "Starting deployment for environment: $ENVIRONMENT"
    log_info "Version: $VERSION"
    echo ""
    
    # Check prerequisites
    check_prerequisites
    echo ""
    
    # Build backend (if .NET SDK is available and not skipped)
    if command -v dotnet &> /dev/null && [ "$SKIP_BUILD" = false ]; then
        build_backend
        echo ""
    fi
    
    # Run tests (if not skipped)
    if [ "$SKIP_TESTS" = false ] && command -v dotnet &> /dev/null; then
        run_tests
        echo ""
    else
        log_warning "Skipping tests"
        echo ""
    fi
    
    # Build Docker image (if not skipped)
    if [ "$SKIP_BUILD" = false ]; then
        build_docker_image
        echo ""
    else
        log_warning "Skipping Docker build"
        echo ""
    fi
    
    # Push Docker image (if not disabled)
    if [ "$NO_PUSH" = false ] && [ -n "$DOCKER_REGISTRY" ]; then
        push_docker_image
        echo ""
    else
        log_warning "Skipping Docker push"
        echo ""
    fi
    
    # Deploy based on ennt
    case $ENVIRONMENT in
        dev)
            deploy_local
            ;;
        staging|production)
            deploy_remote
            ;;
        *)
            log_error "Unknown environment: $ENVIRONMENT"
            exit 1
            ;;
    esac
    
    echo ""
    cleanup
    echo ""
    
    log_success "Deployment completed successfully!"
    log_info "Environment: $ENVIRONMENT"
    log_info "Version: $VERSION"
    
    if [ "$ENVIRONMENT" = "dev" ]; then
        echo ""
        log_info "Access the application at:"
        log_info "  - Backend API: http://localhost:8080"
        log_info "  - Swagger UI: http://localhost:8080/swagger"
        log_info "  - pgAdmin: http://localhost:5050"
    fi
}

# Run main function
main
