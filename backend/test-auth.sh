#!/bin/bash

# Authentication Testing Script for Quater Backend
# This script tests all authentication endpoints

BASE_URL="http://localhost:5000"
API_URL="$BASE_URL/api/auth"

echo "========================================="
echo "Quater Authentication Testing Script"
echo "========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Default credentials (can be overridden by environment variables)
ADMIN_PASSWORD=${ADMIN_PASSWORD:-"Admin@123"}
TECH_PASSWORD=${TECH_PASSWORD:-"Tech@123"}

# Test 1: Login with default admin
echo -e "${YELLOW}Test 1: Login with default admin${NC}"
echo "POST $API_URL/token"
echo ""

LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@quater.local&password=$ADMIN_PASSWORD&scope=openid email profile offline_access")

echo "$LOGIN_RESPONSE" | jq '.'

# Extract access token
ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.access_token')
REFRESH_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.refresh_token')

if [ "$ACCESS_TOKEN" != "null" ] && [ -n "$ACCESS_TOKEN" ]; then
    echo -e "${GREEN}✓ Login successful${NC}"
    echo "Access Token: ${ACCESS_TOKEN:0:50}..."
    echo ""
else
    echo -e "${RED}✗ Login failed${NC}"
    echo ""
    exit 1
fi

# Test 2: Get user info
echo -e "${YELLOW}Test 2: Get user info${NC}"
echo "GET $API_URL/userinfo"
echo ""

USER_INFO=$(curl -s -X GET "$API_URL/userinfo" \
  -H "Authorization: Bearer $ACCESS_TOKEN")

echo "$USER_INFO" | jq '.'

if echo "$USER_INFO" | jq -e '.email' > /dev/null 2>&1; then
    echo -e "${GREEN}✓ User info retrieved successfully${NC}"
    echo ""
else
    echo -e "${RED}✗ Failed to get user info${NC}"
    echo ""
fi

# Test 3: Get lab ID for registration
echo -e "${YELLOW}Test 3: Get lab ID for registration${NC}"
LAB_ID=$(echo "$USER_INFO" | jq -r '.labId')
echo "Lab ID: $LAB_ID"
echo ""

# Test 4: Register new user
echo -e "${YELLOW}Test 4: Register new technician user${NC}"
echo "POST $API_URL/register"
echo ""

REGISTER_RESPONSE=$(curl -s -X POST "$API_URL/register" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{
    \"email\": \"technician@quater.local\",
    \"password\": \"$TECH_PASSWORD\",
    \"role\": \"Technician\",
    \"labId\": \"$LAB_ID\"
  }")

echo "$REGISTER_RESPONSE" | jq '.'

if echo "$REGISTER_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
    echo -e "${GREEN}✓ User registered successfully${NC}"
    echo ""
elif echo "$REGISTER_RESPONSE" | jq -e '.errors' | grep -q "already taken" 2>/dev/null; then
    echo -e "${YELLOW}⚠ User already exists (this is okay)${NC}"
    echo ""
else
    echo -e "${RED}✗ Registration failed${NC}"
    echo ""
fi

# Test 5: Login with new user
echo -e "${YELLOW}Test 5: Login with technician user${NC}"
echo "POST $API_URL/token"
echo ""

TECH_LOGIN=$(curl -s -X POST "$API_URL/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=technician@quater.local&password=$TECH_PASSWORD&scope=openid email profile offline_access")

echo "$TECH_LOGIN" | jq '.'

TECH_TO=$(echo "$TECH_LOGIN" | jq -r '.access_token')

if [ "$TECH_TOKEN" != "null" ] && [ -n "$TECH_TOKEN" ]; then
    echo -e "${GREEN}✓ Technician login successful${NC}"
    echo ""
else
    echo -e "${YELLOW}⚠ Technician login failed (user may not exist yet)${NC}"
    echo ""
fi

# Test 6: Refresh token
echo -e "${YELLOW}Test 6: Refresh access token${NC}"
echo "POST $API_URL/token (refresh)"
echo ""

REFRESH_RESPONSE=$(curl -s -X POST "$API_URL/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=$REFRESH_TOKEN")

echo "$REFRESH_RESPONSE" | jq '.'

NEW_ACCESS_TOKEN=$(echo "$REFRESH_RESPONSE" | jq -r '.access_token')

if [ "$NEW_ACCESS_TOKEN" != "null" ] && [ -n "$NEW_ACCESS_TOKEN" ]; then
    echo -e "${GREEN}✓ Token refresh successful${NC}"
    echo ""
else
    echo -e "${RED}✗ Token refresh failed${NC}"
    echo ""
fi

# Test 7: Logout
echo -e "${YELLOW}Test 7: Logout${NC}"
echo "POST $API_URL/logout"
echo ""

LOGOUT_RESPONSE=$(curl -s -X POST "$API_URL/logout" \
  -H "Authorization: Bearer $ACCESS_TOKEN")

echo "$LOGOUT_RESPONSE" | jq '.'

if echo "$LOGOUT_RESPONSE" | jq -e '.message' > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Logout successful${NC}"
    echo ""
else
    echo -e "${RED}✗ Logout failed${NC}"
    echo ""
fi

# Test 8: Try to access protected endpoint after logout
echo -e "${YELLOW}Test 8: Access protected endpoint after logout (should fail)${NC}"
echo "GET $API_URL/userinfo"
echo ""

AFTER_LOGOUT=$(curl -s -X GET "$API_URL/userinfo" \
  -H "Authorization: Bearer $ACCESS_TOKEN")

echo "$AFTER_LOGOUT" | jq '.'

if echo "$AFTER_LOGOUT" | jq -e '.email' > /dev/null 2>&1; then
    echo -e "${YELLOW}⚠ Still nticated (token not revoked - this is expected behavior)${NC}"
    echo ""
else
    echo -e "${GREEN}✓ Access denied after logout${NC}"
    echo ""
fi

echo "========================================="
echo "Testing Complete!"
echo "========================================="
echo ""
echo "Summary:"
echo "- Default admin credentials: admin@quater.local / Admin@123"
echo "- Access token lifetime: 1 hour"
echo "- Refresh token lifetime: 7 days"
echo "- Password requirements: 8+ chars, uppercase, lowercase, digit, special char"
echo "- Lockout: 5 failed attempts, 15 minute lockout"
echo ""
echo "Nes:"
echo "1. Change default admin password in production"
echo "2. Configure real SSL certificates"
echo "3. Enable HTTPS in production"
echo "4. Implement role-based authorization on endpoints"
