#!/bin/bash
# Simple script to get a Bearer token for Swagger UI testing

API_URL="${API_URL:-http://localhost:5198}"

# Generate a random code verifier (43-128 characters)
CODE_VERIFIER=$(openssl rand -base64 32 | tr -d "=+/" | cut -c1-43)

# Generate code challenge (SHA256 hash of verifier, base64url encoded)
CODE_CHALLENGE=$(echo -n "$CODE_VERIFIER" | openssl dgst -sha256 -binary | openssl base64 | tr -d "=" | tr "/+" "_-")

echo "=========================================="
echo "Get Bearer Token for Swagger UI"
echo "=========================================="
echo ""
echo "Step 1: Open this URL in your browser:"
echo ""
echo "${API_URL}/api/auth/authorize?response_type=code&client_id=quater-mobile-client&redirect_uri=http://127.0.0.1/callback&code_challenge=${CODE_CHALLENGE}&code_challenge_method=S256&scope=openid%20profile%20email%20api%20offline_access"
echo ""
echo "Step 2: Login with your credentials"
echo ""
echo "Step 3: After redirect, copy the 'code' parameter from the URL"
echo "        (URL will look like: http://127.0.0.1/callback?code=XXXXX)"
echo ""
read -p "Paste the authorization code here: " AUTH_CODE
echo ""
echo "Step 4: Exchanging code for token..."
echo ""

# Exchange authorization code for access token
RESPONSE=$(curl -s -X POST "${API_URL}/api/auth/token" \
  -H 'Content-Type: application/x-www-form-urlencoded' \
  -d "grant_type=authorization_code" \
  -d "client_id=quater-mobile-client" \
  -d "code=${AUTH_CODE}" \
  -d "redirect_uri=http://127.0.0.1/callback" \
  -d "code_verifier=${CODE_VERIFIER}")

# Extract access token
ACCESS_TOKEN=$(echo "$RESPONSE" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$ACCESS_TOKEN" ]; then
    echo "❌ Failed to get token. Response:"
    echo "$RESPONSE" | jq . 2>/dev/null || echo "$RESPONSE"
    exit 1
fi

echo "✅ Success! Your Bearer token:"
echo ""
echo "Bearer ${ACCESS_TOKEN}"
echo ""
echo "=========================================="
echo "Copy the line above and paste it into"
echo "Swagger UI's 'Authorize' dialog"
echo "=========================================="
