# Email System Quickstart

## 1. Configuration (`appsettings.json`)

To enable email functionality, add the `Email` section to your `appsettings.json` (or `appsettings.Development.json`).

```json
{
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 2525,
    "SmtpUser": "",
    "SmtpPass": "",
    "EnableSsl": false,
    "FromEmail": "noreply@quater.local",
    "FromName": "Quater Lab System"
  }
}
```

## 2. Local Testing (Mock SMTP)

For local development, do not use real email providers. Use a mock SMTP server that intercepts emails and displays them in a web UI.

### Option A: Smtp4Dev (Docker) - Recommended

Run the following command to start Smtp4Dev:

```bash
docker run --rm -it -p 3000:80 -p 2525:25 rnwood/smtp4dev
```

- **SMTP Port**: `2525` (matches config above)
- **Web UI**: [http://localhost:3000](http://localhost:3000)

### Option B: Papercut (Windows)

If you are on Windows and prefer a native app:
1. Download [Papercut SMTP](https://github.com/ChangemakerStudios/Papercut-SMTP).
2. Run it. It defaults to port `25`.
3. Update `SmtpPort` in `appsettings.json` to `25`.

## 3. Running the Worker

The email system uses a hosted background service. Ensure the backend is running:

```bash
cd backend/src/Quater.Backend.Api
dotnet run
```

Trigger an email (e.g., via Swagger `/api/v1/auth/forgot-password`) and check http://localhost:3000 to see the result.
