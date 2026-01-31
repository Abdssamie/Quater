#!/bin/bash

################################################################################
# Quater Backend - Health Check Script
# 
# This script performs health checks on the Quater backend API.
# Useful for monitoring and deployment verification.
################################################################################

set -e

# Configuration
API_URL="${API_URL:-http://localhost:8080}"
MAX_RETRIES="${MAX_RETRIES:-30}"
RETRY_INTERVAL="${RETRY_INTERVAL:-5}"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

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

# Check liveness endpoint
check_liveness() {
    local url="$API_URL/api/health/live"
    log_info "Checking liveness: $url"
    
    if curl -f -s "$url" > /dev/null; then
        log_success "Liveness check passed"
        return 0
    else
        log_error "Liveness check failed"
        return 1
    fi
}

# Check readiness endpoint
check_readiness() {
    local url="$API_URL/api/health/ready"
    log_info "Checking readiness: $url"
    
    local response=$(curl -s -w "\n%{http_code}" "$url")
    local body=$(echo "$response" | head -n -1)
    local status=$(echo "$response" | tail -n 1)
    
    if [ "$status" = "200" ]; then
        log_success "Readiness check passed"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
        return 0
    else
        log_error "Readiness check failed (HTTP $status)"
        echo "$body"
        return 1
    fi
}

# Check detailed health endpoint
check_health() {
    local url="$API_URL/api/health"
    log_info "Checking detailed health: $url"
    
    local response=$(curl -s -w "\n%{http_code}" "$url")
    local body=$(echo "$response" | head -n -1)
    local status=$(echo "$response" | tail -n 1)
    
    if [ "$status" = "200" ]; then
        log_success "Health check passed"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
        return 0
    else
        log_warning "Health check returned HTTP $status"
        echo "$body"
        return 1
    fi
}

# Wait for service to be ready
wait_for_ready() {
    log_info "Waiting for service to be ready (max ${MAX_RETRIES} attempts)..."
    
    local attempt=1
    while [ $attempt -le $MAX_RETRIES ]; do
        log_info "Attempt $attempt/$MAX_RETRIES..."
        
        if check_liveness; then
            log_success "Service is ready!"
            return 0
        fi
        
        if [ $attempt -lt $MAX_RETRIES ]; then
            log_info "Retrying in ${RETRY_INTERVAL} seconds..."
            sleep $RETRY_INTERVAL
        fi
        
        attempt=$((attempt + 1))
    done
    
    log_error "Service did not become ready after $MAX_RETRIES attempts"
    return 1
}

# Main function
main() {
    echo "========================================"
    echo "Quater Backend - Health Check"
    echo "========================================"
    echo "API URL: $API_URL"
    echo ""
    
    case "${1:-all}" in
        liveness)
            check_liveness
            ;;
        readiness)
            check_readiness
            ;;
        health)
            check_health
            ;;
        wait)
            wait_for_ready
            ;;
        all)
            check_liveness && echo ""
            check_readiness && echo ""
            check_health
            ;;
        *)
            echo "Usage: $0 {liveness|readiness|health|wait|all}"
            echo ""
            echo "Commands:"
            echo "  liveness   - Check if service is alive"
            echo "  readiness  - Check if service is ready to accept traffic"
            echo "  health     - Get detailed health status"
            echo "  wait       - Wait for service to become ready"
            echo "  all        - Run all health checks (default)"
            exit 1
            ;;
    esac
}

# Run main function
main "$@"
