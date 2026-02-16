-- Reset admin password to 'Admin123!'
-- Run this against your PostgreSQL database

-- This updates the password hash for the admin user
-- New password: Admin123!
-- The hash below is for "Admin123!" using the default Identity password hasher

UPDATE "AspNetUsers" 
SET "PasswordHash" = 'AQAAAAIAAYagAAAAEJmH+IWJy5mE2VwL/1BN5T8D0FbGOqVrL1Fqq2zA4fDWJw8vqYKB8BtJ5T6X6K5n2g==',
    "SecurityStamp" = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    "ConcurrencyStamp" = 'b2c3d4e5-f6g7-8901-bcde-f23456789012'
WHERE "Email" = 'admin@quater.local';

-- Verify the update
SELECT "Email", "UserName", "PasswordHash" IS NOT NULL as "HasPassword"
FROM "AspNetUsers" 
WHERE "Email" = 'admin@quater.local';