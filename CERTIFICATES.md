# OpenIddict Certificate Configuration

## Overview

OpenIddict requires two X.509 certificates for production:
- **Encryption Certificate**: Encrypts tokens and sensitive data
- **Signing Certificate**: Signs JWT tokens

## Development

In development, OpenIddict automatically generates ephemeral certificates. No configuration needed.

## Production

### Configuration

Set these in `appsettings.Production.json` or environment variables:

```json
{
  "OpenIddict": {
    "EncryptionCertificatePath": "/app/certs/encryption.pfx",
    "EncryptionCertificatePassword": "${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}",
    "SigningCertificatePath": "/app/certs/signing.pfx",
    "SigningCertificatePassword": "${OPENIDDICT_SIGNING_CERT_PASSWORD}"
  }
}
```

### Generate Certificates

Using OpenSSL:

```bash
# Generate encryption certificate
openssl req -x509 -newkey rsa:4096 -keyout encryption-key.pem -out encryption-cert.pem -days 730 -nodes -subj "/CN=Encryption"
openssl pkcs12 -export -out encryption.pfx -inkey encryption-key.pem -in encryption-cert.pem -password pass:YourPassword

# Generate signing certificate
openssl req -x509 -newkey rsa:4096 -keyout signing-key.pem -out signing-cert.pem -days 730 -nodes -subj "/CN=Signing"
openssl pkcs12 -export -out signing.pfx -inkey signing-key.pem -in signing-cert.pem -password pass:YourPassword
```

### Docker Deployment

Mount certificates as volumes:

```yaml
services:
  api:
    image: quater-backend
    volumes:
      - ./certs:/app/certs:ro
    environment:
      - OPENIDDICT_ENCRYPTION_CERT_PASSWORD=${ENCRYPTION_PASSWORD}
      - OPENIDDICT_SIGNING_CERT_PASSWORD=${SIGNING_PASSWORD}
```

### Security

- ✅ Use strong passwords (32+ characters)
- ✅ Store passwords in secrets management (not in code)
- ✅ Restrict file permissions: `chmod 600 *.pfx`
- ✅ Rotate certificates every 1-2 years
- ❌ Never commit certificates to source control

## Staging Certificates

Pre-generated staging certificates are available in `/certs/staging/` (not committed to git).

See `/certs/staging/README.md` for details.
