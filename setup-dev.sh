#!/bin/bash

################################################################################
# Quater Backend - Local Development Setup Script
# 
# This script sets up the local development environment for the Quater backend.
# It checks prerequisites, creates necessary directories, and starts services.
################################################################################

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

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

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker Desktop."
        exit 1
    fi
    
    # Check Docker Compose
    if ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not available. Please update Docker Desktop."
        exit 1
    fi
    
    # Check .NET SDK (optional for local development)
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        log_success ".NET SDK $DOTNET_VERSION is installed"
    else
        log_warning ".NET SDK not found. You can still use Docker for development."
    fi
    
    log_success "Prerequisites check passed"
}

# Create .env file if it doesn't exist
setup_env_file() {
    log_info "Setting up environment configuration..."
    
    if [ ! -f .env ]; then
        if [ -f .env.example ]; then
            cp .env.example .env
            log_success "Created .env file from .env.example"
            log_warning "Please review and update .env with your configuration"
        else
            log_warning ".env.example not found. Skipping .env creation."
        fi
    else
        log_info ".env file already exists"
    fi
}

# Create necessary directories
create_directories() {
    log_info "Creating necessary directories..."
    
    mkdir -p logs
    mkdir -p backups
    
    log_success "Directories created"
}

# Start services
start_services() {
    log_info "Starting services with Docker Compose..."
    
    cd docker
    docker compose up -d
    
    log_info "Waiting for services to be healthy..."
    sleep 10
    
    # Check service health
    if docker compose ps | grep -q "Up"; then
        log_success "Services are running!"
        echo ""
        docker compose ps
        echo ""
        log_info "Access the application at:"
        log_info "  - Backend API: http://localhost:8080"
        log_info "  - Swagger UI: http://localhost:8080/swagger"
        log_info "  - pgAdmin: http://localhost:5050 (admin@quater.local / admin)"
        echo ""
        log_info "To view logs: docker compose logs -f backend"
        log_info "To stop services: docker compose down"
    else
        log_error "Some services failed to start. Check logs with: docker compose logs"
        exit 1
    fi
}

# Main function
main() {
    echo "=================================="
    echo "Quater Backend - Development Setup"
    echo "=================================="
    echo ""
    
    check_prerequisites
    echo ""
    
    setup_env_file
    echo ""
    
    create_directories
    echo ""
    
    start_services
    echo ""
    
    log_success "Development environment is ready!"
}

# Run main function
main
