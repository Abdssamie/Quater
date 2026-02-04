#!/bin/bash
# Build script for Quater Docker image
# This script must be run from the repository root

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default values
VERSION="${1:-0.1.0-alpha}"
IMAGE_NAME="quater"
DOCKERFILE="docker/Dockerfile"

# Check if we're in the repository root
if [ ! -f "Quater.sln" ]; then
    echo -e "${RED}Error: This script must be run from the repository root${NC}"
    echo -e "${YELLOW}Current directory: $(pwd)${NC}"
    echo -e "${YELLOW}Expected files: Quater.sln, backend/, shared/${NC}"
    exit 1
fi

echo -e "${GREEN}Building Docker image: ${IMAGE_NAME}:${VERSION}${NC}"
echo -e "${YELLOW}Build context: $(pwd)${NC}"
echo -e "${YELLOW}Dockerfile: ${DOCKERFILE}${NC}"

# Build the image
docker build \
    -f "${DOCKERFILE}" \
    -t "${IMAGE_NAME}:${VERSION}" \
    -t "${IMAGE_NAME}:latest" \
    .

echo -e "${GREEN}✓ Build completed successfully!${NC}"
echo -e "${GREEN}Image tags:${NC}"
echo -e "  - ${IMAGE_NAME}:${VERSION}"
echo -e "  - ${IMAGE_NAME}:latest"
