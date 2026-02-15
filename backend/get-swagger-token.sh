#!/bin/bash
# Quick script to get a Bearer token for Swagger UI testing

API_URL="${API_URL:-http://localhost:5198}"
EMAIL="${EMAIL:-admin@quater.local}"
PASSWORD="${PASSWORD:-Admin123!}"

echo "Getting token for Swagger UI..."
echo "Email: $EMAIL"
echo ""

# Step 1: Login to get authorization code (this won't work in script, but shows the flow)
# You need to do this manually in browser or use password grant

# For now, let's try to authenticate directly via the token endpoint
# Note: This requires password grant to be enabled

echo "To get a token for Swagger:"
echo ""
echo "1. Open your browser to: $API_URL/Account/Login"
echo "2. Login with: $EMAIL / $PASSWORD"
echo "3. Then navigate to: $API_URL/api/auth/authorize?response_type=code&client_id=quater-mobile-client&redirect_uri=http://127.0.0.1/callback&code_challenge=test123&code_challenge_method=S256&scope=openid%20profile%20email%20api%20offline_access"
echo "4. Copy the 'code' parameter from the redirect URL"
echo "5. Exchange it for a token using:"
echo ""
echo "curl -X POST $API_URL/api/auth/token \\"
echo "  -H 'Content-Type: application/x-www-form-urlencoded' \\"
echo "  -d 'grant_type=authorization_code' \\"
echo "  -d 'client_id=quater-mobile-client' \\"
echo "  -d 'code=YOUR_CODE_HERE' \\"
echo "  -d 'redirect_uri=http://127.0.0.1/callback' \\"
echo "  -d 'code_verifier=test123'"
echo ""
echo "6. Copy the 'access_token' from the response"
echo "7. In Swagger UI, click 'Authorize' and paste: Bearer YOUR_ACCESS_TOKEN"
