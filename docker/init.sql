-- Create default lab
INSERT INTO "Labs" ("Id", "Name", "Location", "ContactInfo", "CreatedDate", "IsActive")
VALUES ('f47ac10b-58cc-4372-a567-0e02b2c3d479', 'Default Lab', 'Main Campus', 'admin@quater.local', NOW(), TRUE)
ON CONFLICT DO NOTHING;

-- Create parameters based on WHO standards
INSERT INTO "Parameters" ("Id", "Name", "Unit", "Description", "IsActive", "CreatedDate", "LastModified")
VALUES
('a1b2c3d4-e5f6-7890-1234-567890abcdef', 'pH', '', 'Measure of acidity or alkalinity', TRUE, NOW(), NOW()),
('b2c3d4e5-f678-9012-3456-7890abcdef12', 'Turbidity', 'NTU', 'Cloudiness or haziness of a fluid', TRUE, NOW(), NOW()),
('c3d4e5f6-7890-1234-5678-90abcdef1234', 'Chlorine', 'mg/L', 'Residual chlorine concentration', TRUE, NOW(), NOW()),
('d4e5f678-9012-3456-7890-abcdef123456', 'Nitrate', 'mg/L', 'Concentration of nitrate ions', TRUE, NOW(), NOW()),
('e5f67890-1234-5678-90ab-cdef12345678', 'Lead', 'mg/L', 'Concentration of lead', TRUE, NOW(), NOW())
ON CONFLICT DO NOTHING;
