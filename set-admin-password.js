// Script to set admin password via API registration endpoint
const bcrypt = require('bcryptjs');

// Generate password hash for Test123!
const password = 'Test123!';
const hash = bcrypt.hashSync(password, 10);

console.log('Password:', password);
console.log('Hash:', hash);

// SQL to update admin user with proper Identity fields
const sql = `
UPDATE public."Users" 
SET 
  "UserName" = 'admin@witchcityrope.com',
  "NormalizedUserName" = 'ADMIN@WITCHCITYROPE.COM',
  "NormalizedEmail" = 'ADMIN@WITCHCITYROPE.COM',
  "EmailConfirmed" = true,
  "PasswordHash" = '${hash}',
  "SecurityStamp" = '${crypto.randomUUID()}',
  "ConcurrencyStamp" = '${crypto.randomUUID()}',
  "LockoutEnabled" = true,
  "AccessFailedCount" = 0
WHERE "Email" = 'admin@witchcityrope.com';
`;

console.log('SQL to execute:');
console.log(sql);