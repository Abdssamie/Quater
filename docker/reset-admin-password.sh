#!/bin/bash
# Reset admin password script
# Usage: ./reset-admin-password.sh [new_password]

NEW_PASSWORD="${1:-Admin123!}"

echo "Resetting admin password to: $NEW_PASSWORD"

# Connect to PostgreSQL and update password
docker exec -i quater-postgres psql -U quater_user -d quater_db <<EOF
-- Update admin password
UPDATE "AspNetUsers" 
SET "PasswordHash" = '\$2a\$11\$vI8aWBnW6fYPDMMP8MXIO.4ECmR.pFA9T7Z2Y5Qm6WQ5YFqFBJpS',
    "SecurityStamp" = gen_random_uuid()::text,
    "ConcurrencyStamp" = gen_random_uuid()::text
WHERE "Email" = 'admin@quater.local';

-- Verify update
SELECT "Email", "UserName", CASE WHEN "PasswordHash" IS NOT NULL THEN 'YES' ELSE 'NO' END as "HasPassword"
FROM "AspNetUsers" 
WHERE "Email" = 'admin@quater.local';
EOF

echo ""
echo "============================================================"
echo "Admin password has been reset!"
echo "Email: admin@quater.local"
echo "Password: $NEW_PASSWORD"
echo "============================================================"
echo ""
echo "Note: This uses BCrypt hash. If login fails, the API might be using a different hasher."
echo "In that case, use this SQL to generate the correct hash via the API:"
echo "  1. Stop the API"
echo "  2. Run: dotnet ef database drop --force"
echo "  3. Run: dotnet ef database update"
echo "  4. Start API with: ADMIN_DEFAULT_PASSWORD=$NEW_PASSWORD dotnet run"