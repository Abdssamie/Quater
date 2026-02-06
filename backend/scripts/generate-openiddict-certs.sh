#!/bin/bash
# =============================================================================
# OpenIddict Certificate Generation Script
# =============================================================================
# This script generates self-signed certificates for OpenIddict in development
# and provides instructions for production certificate generation.
#
# Usage:
#   ./generate-openiddict-certs.sh [environment]
#
# Arguments:
#   environment: "development" or "production" (default: development)
#
# Output:
#   - Development: Self-signed certificates in ./certs/
#   - Production: Certificate signing requests (CSRs) in ./certs/
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT="${1:-development}"
CERT_DIR="./certs"
VALIDITY_DAYS=365
KEY_SIZE=2048

# Certificate details
COUNTRY="US"
STATE="State"
CITY="City"
ORG="Quater"
OU="Development"
CN_ENCRYPTION="Quater Encryption Certificate"
CN_SIGNING="Quater Signing Certificate"

# =============================================================================
# Functions
# =============================================================================

print_header() {
    echo -e "${BLUE}=============================================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}=============================================================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ $1${NC}"
}

# =============================================================================
# Main Script
# =============================================================================

print_header "OpenIddict Certificate Generation - ${ENVIRONMENT^^}"

# Create certificate directory
if [ ! -d "$CERT_DIR" ]; then
    mkdir -p "$CERT_DIR"
    print_success "Created certificate directory: $CERT_DIR"
fi

# Check if openssl is installed
if ! command -v openssl &> /dev/null; then
    print_error "OpenSSL is not installed. Please install it first."
    exit 1
fi

# =============================================================================
# Development Certificates (Self-Signed)
# =============================================================================

if [ "$ENVIRONMENT" == "development" ]; then
    print_info "Generating self-signed certificates for development..."
    echo ""

    # Generate Encryption Certificate
    print_info "Generating encryption certificate..."
    openssl req -x509 -newkey rsa:$KEY_SIZE -keyout "$CERT_DIR/encryption-key.pem" \
        -out "$CERT_DIR/encryption-cert.pem" -days $VALIDITY_DAYS -nodes \
        -subj "/C=$COUNTRY/ST=$STATE/L=$CITY/O=$ORG/OU=$OU/CN=$CN_ENCRYPTION" \
        2>/dev/null

    # Convert to PFX
    openssl pkcs12 -export -out "$CERT_DIR/encryption.pfx" \
        -inkey "$CERT_DIR/encryption-key.pem" -in "$CERT_DIR/encryption-cert.pem" \
        -passout pass: 2>/dev/null

    print_success "Encryption certificate generated: $CERT_DIR/encryption.pfx"

    # Generate Signing Certificate
    print_info "Generating signing certificate..."
    openssl req -x509 -newkey rsa:$KEY_SIZE -keyout "$CERT_DIR/signing-key.pem" \
        -out "$CERT_DIR/signing-cert.pem" -days $VALIDITY_DAYS -nodes \
        -subj "/C=$COUNTRY/ST=$STATE/L=$CITY/O=$ORG/OU=$OU/CN=$CN_SIGNING" \
        2>/dev/null

    # Convert to PFX
    openssl pkcs12 -export -out "$CERT_DIR/signing.pfx" \
        -inkey "$CERT_DIR/signing-key.pem" -in "$CERT_DIR/signing-cert.pem" \
        -passout pass: 2>/dev/null

    print_success "Signing certificate generated: $CERT_DIR/signing.pfx"

    # Clean up PEM files
    rm -f "$CERT_DIR/encryption-key.pem" "$CERT_DIR/encryption-cert.pem"
    rm -f "$CERT_DIR/signing-key.pem" "$CERT_DIR/signing-cert.pem"

    echo ""t_success "Development certificates generated successfully!"
    echo ""
    print_warning "These are self-signed certificates for DEVELOPMENT ONLY."
    print_warning "DO NOT use these certificates in production!"
    echo ""
    print_info "Certificate files:"
    echo "  - $CERT_DIR/encryption.pfx (no password)"
    echo "  - $CERT_DIR/signing.pfx (no password)"
    echo ""
    print_info "To use these certificates:"
    echo "  1. Set environment variables:"
    echo "     export OPENIDDICT_ENCRYPTION_CERT_PATH=\"$(pwd)/$CERT_DIR/encryption.pfx\""
    echo "     export OPENIDDICT_SIGNING_CERT_PATH=\"$(pwd)/$CERT_DIR/signing.pfx\""
    echo "     export OPENIDDICT_ENCRYPTION_CERT_PASSWORD=\"\""
    echo "     export OPENIDDICT_SIGNING_CERT_PASSWORD=\"\""
    echo ""
    echo "  2. Or update appsettings.Development.json:"
    echo "     \"OpenIddict\": {"
    echo "       \"EncryptionCertificatePath\": \"$(pwd)/$CERT_DIR/encryption.pfx\","
    echo "       \"SigningCertificatePath\": \"$(pwd)/$CERT_DIR/signing.pfx\""
    echo "     }"
    echo ""

# =============================================================================
# Production Certificates (CSR Generation)
# =============================================================================

elif [ "$ENVIRONMENT" == "production" ]; then
    print_info "Generating certificate signing requests (CSRs) for production..."
    echo ""

    # Generate Encryption Certificate CSR
    print_info "Generating encryption certificate private key and CSR..."
    openssl genrsa -out "$CERT_DIR/encryption-key.pem" $KEY_SIZE 2>/dev/null
    openssl req -new -key "$CERT_DIR/encryption-key.pem" \
        -out "$CERT_DIR/encryption.csr" \
        -subj "/C=$COUNTRY/ST=$STATE/L=$CITY/O=$ORG/OU=Production/CN=$CN_ENCRYPTION" \
        2>/dev/null

    print_success "Encryption CSR generated: $CERT_DIR/encryption.csr"

    # Generate Signing Certificate CSR
    print_info "Generating signing certificate private key and CSR..."
    openssl genrsa -out "$CERT_DIR/signing-key.pem" $KEY_SIZE 2>/dev/null
    openssl req -new -key "$CERT_DIR/signing-key.pem" \
        -out "$CERT_DIR/signing.csr" \
        -subj "/C=$COUNTRY/ST=$STATE/L=$CITY/O=$ORG/OU=Production/CN=$CN_SIGNING" \
        2>/dev/null

    print_success "Signingated: $CERT_DIR/signing.csr"

    echo ""
    print_success "Production CSRs generated successfully!"
    echo ""
    print_warning "Next steps for production deployment:"
    echo ""
    echo "1. Submit CSRs to your Certificate Authority (CA):"
    echo "   - $CERT_DIR/encryption.csr"
    echo "   - $CERT_DIR/signing.csr"
    echo ""
    echo "2. Once you receive the signed certificates from your CA:"
    echo "   - Save them as encryption-cert.pem and signing-cert.pem"
    echo ""
    echo "3. Create PFX files with passwords:"
    echo "   openssl pkcs12 -export -out encryption.pfx \\"
    echo "     -inkey $CERT_DIR/encryption-key.pem \\"
    echo "     -in encryption-cert.pem \\"
    echo "     -passout pass:YOUR_STRONG_PASSWORD"
    echo ""
    echo "   openssl pkcs12 -export -out signing.pfx \\"
    echo "     -inkey $CERT_DIR/signing-key.pem \\"
    echo "     -in signing-cert.pem \\"
    echo "     -passout pass:YOUR_STRONG_PASSWORD"
    echo ""
    echo "4. Store certificates securely in Infisical:"
    echo "   infisical secrets set OPENIDDICT_ENCRYPTION_CERT_PATH /app/certs/encryption.pfx"
    echo "   increts set OPENIDDICT_ENCRYPTION_CERT_PASSWORD YOUR_STRONG_PASSWORD"
    echo "   infisical secrets set OPENIDDICT_SIGNING_CERT_PATH /app/certs/signing.pfx"
    echo "   infisical secrets set OPENIDDICT_SIGNING_CERT_PASSWORD YOUR_STRONG_PASSWORD"
    echo ""
    echo "5. Mount certificates in Docker:"
    echo "   volumes:"
    echo "     - ./certs:/app/certs:ro"
    echo ""
    print_warning "IMPORTANT: Keep private keys secure and never commit them to version control!"
    echo ""

else
    print_error "Invalid environment: $ENVIRONMENT"
    echo "Usage: $0 [development|production]"
    exit 1
fi

print_header "Certificate Generation Complete"
