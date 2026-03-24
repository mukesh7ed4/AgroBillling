INSERT INTO AdminUsers (FullName, Email, PasswordHash, IsActive, CreatedAt)
VALUES (
  'Mukesh Admin',
  'admin@agrobilling.com',
  LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'admin123'), 2)),
  1,
  GETDATE()
);