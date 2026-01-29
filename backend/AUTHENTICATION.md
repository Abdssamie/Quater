# Authentication Implementation Summary

## What Was Implemented

### 1. **NuGet Packages Added**
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (10.0.2) - User management
- `OpenIddict.AspNetCore` (5.8.0) - OAuth2/OpenID Connect server
- `OpenIddict.EntityFrameworkCore` (5.8.0) - OpenIddict EF Core integration

### 2. **Database Migration**
- Created migration `AddOpenIddict` to add OpenIddict tables:
  - `OpenIddictApplications` - OAuth2 client applications
  - `OpenIddictAuthorizations` - Authorization grants
  - `OpenIddictScopes` - OAuth2 scopes
  - `OpenIddictTokens` - Access and refresh tokens

### 3. **Identity Configuration** (Program.cs)
```csharp
// Password Requirements
- Minimum 8 characters
- Requires uppercase letter
- Requires lowercase letter
- Requires digit
- Requires special character

// Lockout Settings
- 5 failed attempts before lockout
- 15 minute lockout duration
- Enabled for new users

// User Settings
- Unique email required
```

### 4. **OpenIddict Configuration** (Program.cs)
```csharp
// Token Endpoint
- POST /api/auth/token

// Flows Enabled
- Password flow (username/password)
- Refresh token flow

// Token Lifetimes
- Access token: 1 hour
- Refresh token: 7 days

// Signing Algorithm
- RS256 (development certificates for now)
- TODO: Use real certificates in production
```

### 5. **Authentication Endpoints** (AuthController)

#### **POST /api/auth/register**
Register a new user.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass@123",
  "role": "Technician",
  "labId": "00000000-0000-0000-0000-000000000000"
}
```

**Response (201 Created):**
```json
{
  "id": "user-id",
  "email": "user@example.com",
  "role": "Technician",
  "labId": "00000000-0000-0000-0000-000000000000",
  "isActive": true,
  "lastLogin": null
}
```

#### **POST /api/auth/token**
Issue access token and refresh token (OAuth2 password flow).

**Request (form-urlencoded):**
```
grant_type=password
username=admin@quater.local
password=Admin@123
scope=openid email profile offline_access
```

**Response (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "CfDJ8..."
}
```

#### **POST /api/auth/token** (Refresh)
Refresh access token using refresh token.

**Request (form-urlencoded):**
```
grant_type=refresh_token
refresh_token=CfDJ8...
```

**Response (200 OK):**
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "CfDJ8..."
}
```

#### **GET /api/auth/userinfo**
Get current user information (requires authentication).

**Headers:**
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "id": "user-id",
  "email": "admin@quater.local",
  "role": "Admin",
  "labId": "00000000-0000-0000-0000-000000000000",
  "isActive": true,
  "lastLogin": "2026-01-27T12:00:00Z"
}
```

#### **POST /api/auth/logout**
Logout user (requires authentication).

**Headers:**
```
Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "message": "Logged out successfully"
}
```

### 6. **Database Seeding** (DatabaseSeeder)

On application startup, the following data is automatically seeded:

#### **Default Lab**
- Name: "Default Lab"
- Location: "Default Location"
- Contact: "contact@quater.local"

#### **Default Admin User**
- Email: `admin@quater.local`
- Password: `Admin@123` ⚠️ **CHANGE IN PRODUCTION!**
- Role: Admin

#### **Default Parameters** (10 WHO standards)
- pH (6.5-8.5)
- Turbidity (≤5 NTU)
- Free Chlorine (0.2-5 mg/L)
- Total Chlorine (≤5 mg/L)
- E. coli (0 CFU/100mL)
- Total Coliforms (0 CFU/100mL)
- Temperature
- Conductivity
- Dissolved Oxygen
- Hardness

---

## Configuration Changes Needed

### 1. **Connection String** (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=quater;Username=postgres;Password=postgres"
  }
}
```

### 2. uction Certificates** (Program.cs)
Replace development certificates with real certificates:
```csharp
// In production
options.AddEncryptionCertificate(certificate)
       .AddSigningCertificate(certificate);
```

### 3. **HTTPS Configuration**
Ensure HTTPS is enforced in production for secure token transmission.

---

## How to Test

### Prerequisites
1. PostgreSQL database running
2. Run migrations: `dotnet ef database update --project src/Quater.Backend.Data --startup-project src/Quater.Backend.Api`
3. Start the API: `dotnet run --project src/Quater.Backend.Api`

### Test 1: Login with Default Admin
```bash
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=admin@quater.local&password=Admin@123&scope=openid email profile offline_access"
```

**Expected Response:**
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "CfDJ8..."
}
```

### Test 2: Get User Info
```bash
curl -X GET http://localhost:5000/api/auth/userinfo \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**Expected Response:**
```json
{
  "id": "...",
  "email": "admin@quater.local",
  "role": "Admin",
  "labId": "...",
  "isActive": true,
  "lastLogin": "2026-01-27T..."
}
```

### Test 3: Register New User
First, get the Lab ID from the database or use the seeded lab ID.

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "technician@quater.local",
    "password": "Tech@123",
    "role": "Technician",
    "labId": "YOUR_LAB_ID"
  }'
```

**Expected Response (201 Created):**
```json
{
  "id": "...",
  "email": "technician@quater.local",
  "role": "Technician",
  "labId": "...",
  "isActive": true,
  "lastLogin": null
}
```

### Test 4: Refresh Token
```bash
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=YOUR_REFRESH_TOKEN"
```

**Expected Response:**
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "CfDJ8..."
}
```

### Test 5: Logout
```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

**Expected Response:**
```json
{
  "message": "Logged out successfully"
}
```

### Test 6: Access Protected Endpoint
Try accessing a protected endpoint (e.g., samples) with the token:

```bash
curl -X GET http://localhost:5000/api/samples \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## Testing with Swagger UI

1. Navigate to `http://localhost:5000/swagger`
2. Click "Authorize" button
3. Use the default admin credentials:
   - Username: `admin@quater.local`
   - Password: `Admin@123`
4. Test all endpoints interactively

---

## Security Notes

### ⚠️ **IMPORTANT: Production Security**

1. **Change Default Admin Password**
   - The default password `Admin@123` is for development only
   - Change immediately after first login in production

2. **Use Real Certificates**
   - Replace development certificates with production certificates
   - Use Let's Encrypt or purchase SSL certificates

3. **Enable HTTPS**
   - Always use HTTPS in production
   - Redirect HTTP to HTTPS

4. **Token Storage**
   - Store access tokens in memory (not localStorage)
   - Store refresh tokens in secure HTTP-only cookies or secure storn
5. **Token Validation**
   - Tokens are validated on every request
   - Expired tokens are automatically rejected
   - Refresh tokens have 7-day grace period for offline scenarios

---

## JWT Token Claims

The access token includes the following claims:

```json
{
  "sub": "user-id",
  "name": "user@example.com",
  "email": "user@example.com",
  "role": "Admin",
  "lab_id": "00000000-0000-0000-0000-000000000000",
  "exp": 1706356800,
  "iss": "http://localhost:5000",
  "aud": "http://localhost:5000"
}
```

---

## Troubleshooting

### Issue: "The OpenID Connt cannot be retrieved"
**Solution:** Ensure you're sending the request as `application/x-www-form-urlencoded`, not JSON.

### Issue: "The username or password is invalid"
**Solution:** 
- Check that the database has been seeded
- Verify the username is `admin@quater.local` (not just `admin`)
- Verify the password is `Admin@123` (case-sensitive)

### Issue: "Account is locked out"
**Solution:** Wait 15 minutes or reset the lockout in the database.

### Issue: "Lab not found" during registration
**Solution:** Ensure the database has been seeded with the default lab, or create a lab first.

---

## Next Steps

1. ✅ Authentication implemented
2. ⏳ Protect API endpoints with `[Authorize]` attribute
3. ⏳ Implement role-based authorization (`[Authorize(Roles = "Admin")]`)
4. ⏳ Add password reset functionality
5. ⏳ Add email confirmation
6. ⏳ Implement token revocation
7. ⏳ Add audit logging for authentication events

---

**Implementation Status:** ✅ Complete - Ready for testing
