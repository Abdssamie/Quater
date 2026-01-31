# OpenIddict Certificate Configuration Guide

This guide explains how to generate and configure X.509 certificates for OpenIddict in production environments.

## Overview

OpenIddict requires two certificates:
- **Signing Certificate**: Used to sign JWT tokens
- **Encryption Certificate**: Used to encrypt tokens and sensitive data

## Development vs Production

### Development
In development, OpenIddict automatically generates ephemeral certificates:
```csharp
options.AddDevelopmentEncryptionCertificate()
       .AddDevelopmentSigningCertificate();
```
**⚠️ WARNING**: Development certificates are NOT suitable for production as they:
- Are regenerated on each restart (invalidating all existing tokens)
- Are not cryptographically secure
- Cannot be shared across multiple instances

### Production
Production requires persistent, secure certificates loaded via one of three strategies:
1. **Base64 Encoded** (Recommended for containers)
2. **File Path** (Traditional deployments)
3. **Windows Certificate Store** (Windows Server only)

---

## Certificate Generation

### Option 1: Using OpenSSL (Recommended)

#### Generate Signing Certificate
```bash
# Generate private key
openssl genrsa -out signing-key.pem 4096

# Generate certificate signing request
openssl req -new -key signing-key.pem -out signing-csr.pem \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=Quater Signing Certificate"

# Generate self-signed certificate (valid for 10 years)
openssl x509 -req -days 3650 -in signing-csr.pem \
  -signkey signing-key.pem -out signing-cert.pem

# Convert to PFX format with password
openssl pkcs12 -export -out signing.pfx \
  -inkey signing-key.pem -in signing-cert.pem \
  -password pass:YourSecurePassword123!
```

#### Generate Encryption Certificate
```bash
# Generate private key
openssl genrsa -out encryption-key.pem 4096

# Generate certificate signing request
openssl req -new -key encryption-key.pem -out encryption-csr.pem \
  -subj "/C=US/ST=State/L=City/O=Organization/CN=Quater Encryption Certificate"

# Generate self-signed certificate (valid for 10 years)
openssl x509 -req -days 3650 -in encryption-csr.pem \
  -signkey encryption-key.pem -out encryption-cert.pem

# Convert to PFX format with password
openssl pkcs12 -export -out encryption.pfx \
  -inkey encryption-key.pem -in encryption-cert.pem \
  -password pass:YourSecurePassword123!
```

### Option 2: Using PowerShell (Windows)

```powershell
# Generate Signing Certificate
$signingCert = New-SelfSignedCertificate `
    -Subject "CN=Quater Signing Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec Signature `
    -KeyLength 4096 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(10)

# Export to PFX
$password = ConvertTo-SecureString -String "YourSecurePassword123!" -Force -AsPlainText
Export-PfxCertificate -Cert $signingCert -FilePath "signing.pfx" -Password $password

# Generate Encryption Certificate
$encryptionCert = New-SelfSignedCertificate `
    -Subject "CN=Quater Encryption Certificate" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeySpec KeyExchange `
    -KeyLength 4096 `
    -KeyAlgorithm RSA `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(10)

# Export to PFX
Export-PfxCertificate -Cert $encryptionCert -FilePath "encryption.pfx" -Password $password
```

### Option 3: Using .NET CLI

```bash
# Install certificate generation tool
dotnet tool install --global dotnet-certificate

# Generate signing certificate
dotnet certificate create --subject "CN=Quater Signing Certificate" \
  --output signing.pfx --password "YourSecurePassword123!" \
  --key-length 4096 --valid-years 10

# Generate encryption certificate
dotnet certificate create --subject "CN=Quater Encryption Certificate" \
  --output encryption.pfx --password "YourSecurePassword123!" \
  --key-length 4096 --valid-years 10
```

---

## Configuration Strategies

### Strategy 1: Base64 Encoded (Recommended for Docker/Kubernetes)

**Best for**: Docker, Kubernetes, cloud-native deployments

**Advantages**:
- Works seamlessly with container orchestration
- Easy to inject via environment variables or secrets
- No file system dependencies
- Portable across platforms

**Setup**:

1. **Convert PFX to Base64**:
```bash
# Linux/macOS
base64 -i signing.pfx -o signing.pfx.base64
base64 -i encryption.pfx -o encryption.pfx.base64

# Windows PowerShell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("signing.pfx")) | Out-File signing.pfx.base64
[Convert]::ToBase64String([IO.File]::ReadAllBytes("encryption.pfx")) | Out-File encryption.pfx.base64
```

2. **Configure appsettings.Production.json**:
```json
{
  "OpenIddict": {
    "EncryptionCertificateBase64": "${OPENIDDICT_ENCRYPTION_CERT_BASE64}",
    "EncryptionCertificatePassword": "${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}",
    "SigningCertificateBase64": "${OPENIDDICT_SIGNING_CERT_BASE64}",
    "SigningCertificatePassword": "${OPENIDDICT_SIGNING_CERT_PASSWORD}"
  }
}
```

3. **Set Environment Variables**:
```bash
export OPENIDDICT_ENCRYPTION_CERT_BASE64="<base64-content-from-file>"
export OPENIDDICT_ENCRYPTION_CERT_PASSWORD="YourSecurePassword123!"
export OPENIDDICT_SIGNING_CERT_BASE64="<base64-content-from-file>"
export OPENIDDICT_SIGNING_CERT_PASSWORD="YourSecurePassword123!"
```

4. **Docker Compose Example**:
```yaml
services:
  api:
    image: quater-backend:latest
    environment:
      - OPENIDDICT_ENCRYPTION_CERT_BASE64=${OPENIDDICT_ENCRYPTION_CERT_BASE64}
      - OPENIDDICT_ENCRYPTION_CERT_PASSWORD=${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}
      - OPENIDDICT_SIGNING_CERT_BASE64=${OPENIDDICT_SIGN_BASE64}
      - OPENIDDICT_SIGNING_CERT_PASSWORD=${OPENIDDICT_SIGNING_CERT_PASSWORD}
```

5. **Kubernetes Secret Example**:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: openiddict-certificates
type: Opaque
data:
  encryption-cert: <base64-encoded-pfx>
  encryption-password: <base64-encoded-password>
  signing-cert: <base64-encoded-pfx>
  signing-password: <base64-encoded-password>
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: quater-backend
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: OPENIDDICT_ENCRYPTION_CERT_BASE64
          valueFrom:
            secretKeyRef:
              name: openiddict-certificates
              key: encryption-cert
        - name: OPENIDDICT_ENCRYPTION_CERT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: openiddict-certificates
              key: encryption-password
        - name: OPENIDDICT_SIGNING_CERT_BASE64
          valueFrom:
            secretKeyRef:
              name: openiddict-certificates
              key: signing-cert
        - name: OPENIDDICT_SIGNING_CERT_PASSWORD
          valueFrom:
            secretKeyRef:
              name: openiddict-certificates
              key: sing-password
```

---

### Strategy 2: File Path (Traditional Deployments)

**Best for**: VMs, bare metal servers, traditional hosting

**Advantages**:
- Simple and straightforward
- Easy to manage with configuration management tools
- Direct file access

**Setup**:

1. **Copy certificates to server**:
```bash
# Create certificates directory
mkdir -p /app/certs
chmod 700 /app/certs

# Copy certificates (use SCP, SFTP, or configuration management)
scp signing.pfx encryption.pfx user@server:/app/certs/
chmod 600 /app/certs/*.pfx
```

2. **Configure appsettings.Production.json**:
```json
{
  "OpenIddict": {
   yptionCertificatePath": "/app/certs/encryption.pfx",
    "EncryptionCertificatePassword": "${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}",
    "SigningCertificatePath": "/app/certs/signing.pfx",
    "SigningCertificatePassword": "${OPENIDDICT_SIGNING_CERT_PASSWORD}"
  }
}
```

3. **Set password via environment variable**:
```bash
export OPENIDDICT_ENCRYPTION_CERT_PASSWORD="YourSecurePassword123!"
export OPENIDDICT_SIGNING_CERT_PASSWORD="YourSecurePassword123!"
```

---

### Strategy 3: Windows Certificate Store (Windows Server)

**Best for**: Windows Server deployments, IIS hosting

**Advantages**:
- Leverages Windows security infrastructure
- Centralized certificate management
- Automatic renewal support (with proper CA setup)

**Setup**:

1. **Import certificates to Windows Certificate Store**:
```powershell
# Import to LocalMachine\My store
$password = ConvertTo-SecureString -String "YourSecurePassword123!" -Force -AsPlainText
Import-PfxCertificate -FilePath "signing.pfx" -CertStoreLocation "Cert:\LocalMachine\My" -Password $password
Import-PfxCertificate -FilePath "encryption.pfx" -CertStoreLocation "Cert:\LocalMachine\My" -Password $password

# Get thumbprints
Get-ChildItem Cert:\LocalMachine\My | Where-Object {$_.Subject -like "*Quater*"}
```

2. **Configure appsettings.Production.json**:
```json
{
  "OpenIddict": {
    "EncryptionCertificateThumbprint": "1234567890ABCDEF1234567890ABCDEF12345678",
    "EncryptionCertificateStoreName": "My",
    "EncryptionCertificateStoreLocation": "LocalMachine",
    "SigningCertificateThumbprint": "ABCDEF1234567890ABCDEF1234567890ABCDEF12",
    "SigningCertificateStoreName": "My",
    "SigningCertificateStoreLocation": "LocalMachine"
  }
}
```

3. **Grant application pool access** (IIS):
```powershell
# Find the application pool identity
$appPoolName = "QuaterAppPool"
$appPoolIdentity = "IIS AppPool\$appPoolName"

# Grant read access to private keys
$thumbprint = "1234567890ABCDEF1234567890ABCDEF12345678"
$cert = Get-ChildItem Cert:\LocalMachine\My\$thumbprint
$keyPath = $cert.PrivateKey.CspKeyContainerInfo.UniqueKeyContainerName
$keyFullPath = "$env:ProgramData\Microsoft\Crypto\RSA\MachineKeys\$keyPath"
$acl = Get-Acl $keyFullPath
$permission = $appPoolIdentity, "Read", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSyAccessRule $permission
$acl.AddAccessRule($accessRule)
Set-Acl $keyFullPath $acl
```

---

## Security Best Practices

### Certificate Management
1. **Use strong passwords**: Minimum 16 characters with mixed case, numbers, and symbols
2. **Rotate certificates regularly**: Every 1-2 years minimum
3. **Store passwords securely**: Use secret management systems (HashiCorp Vault, AWS Secrets Manager, etc.)
4. **Restrict file permissions**: `chmod 600` for certificate files
5. **Never commit certificates to source control**: Add `*.pfx` to `.gitignore`

### Production Checklist
- [ ] Certificates generated with 4096-bit keys
- [ ] Certificates valid for appropriate duration (1-10 yearsswords stored in secure secret management system
- [ ] File permissions restricted (if using file path strategy)
- [ ] Certificates backed up securely
- [ ] Certificate expiration monitoring configured
- [ ] Renewal process documented

### Monitoring
The `CertificateLoader` class automatically:
- Validates certificates have private keys
- Checks certificate expiration dates
- Warns if certificates expire within 30 days
- Logs certificate loading details

Monitor logs for warnings like:
```
[Warning] Encryption certificate expires in 25 days on 2024-12-31. Please renew soon.
```

---

## Troubleshooting

### "Certificate does not have a private key"
**Cause**: Certificate was exported without private key or imported incorrectly.
**Solution**: Re-export with `-KeyExportPolicy Exportable` (PowerShell) or ensure OpenSSL includes `-inkey` parameter.

### "Failed to load certificate from bytes"
**Cause**: Incorrect password or corrupted certificate file.
**Solution**: Verify password is correct and certificate file is valid PFX/PKCS12 format.

### "Certificate has expired"
**Cause**: Certificate validity period has passed.
**Solution**: Generate new certifieploy them.

### "Certificate with thumbprint 'XXX' not found"
**Cause**: Certificate not installed in specified store location.
**Solution**: Verify certificate is imported to correct store (LocalMachine\My vs CurrentUser\My).

### Base64 decoding errors
**Cause**: Base64 string contains line breaks or invalid characters.
**Solution**: Ensure Base64 string is single-line with no whitespace:
```bash
# Remove line breaks
cat signing.pfx.base64 | tr -d '\n' > signing.pfx.base64.clean
```

---

## Certificate Renewal Process

1. **Generate new certificates** (follow generation steps above)
2. **Test in staging environment** first
3. **Deploy to production** using your chosen strategy
4. **Restart application** to load new certificates
5. **Verify** tokens are being issued correctly
6. **Monitor** for any authentication issues

**Note**: Existing tokens signed with old certificates will remain valid until they expire. Plan renewal during low-traffic periods if possible.

---

## Additional Resources

- [OpenIddict Documentation](https://documentation.openiddict.com/)
- [X.509 Certificate Best Practices](https://www.ssl.com/guide/pem-der-crt-and-cer-x-509-encodings-and-conversions/)
- [ASP.NET Core Certificate Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth)
