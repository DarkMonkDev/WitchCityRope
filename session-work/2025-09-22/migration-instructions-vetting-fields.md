# Migration Instructions: Add EncryptedOtherNames to VettingApplication

## Summary of Changes Made

I have successfully added the `EncryptedOtherNames` field to the vetting system:

### ✅ Completed
1. **Entity Model Updated**: Added `EncryptedOtherNames` property to `VettingApplication.cs`
2. **EF Configuration Updated**: Added field configuration to `VettingApplicationConfiguration.cs`
3. **Patterns Applied**: Following all WitchCityRope database standards (UTC DateTime, encryption, PostgreSQL optimizations)

### 🔄 Remaining Steps
1. **Generate Migration** (commands provided below)
2. **Apply Migration** (commands provided below)
3. **Verify Schema** (validation commands provided below)

## Field Specifications

| Field | Type | Length | Nullable | Encrypted | Purpose |
|-------|------|--------|----------|-----------|---------|
| `EncryptedPronouns` | varchar(200) | 200 | ✅ Yes | ✅ Yes | User's preferred pronouns (EXISTING) |
| `EncryptedOtherNames` | varchar(1000) | 1000 | ✅ Yes | ✅ Yes | Other names/nicknames/handles (NEW) |

## Migration Commands

### 1. Start Docker Environment
```bash
# Start the development environment
./dev.sh

# OR manually:
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up --build -d
```

### 2. Generate Migration
```bash
# Generate the migration for the new field
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations add AddEncryptedOtherNamesToVettingApplication'
```

### 3. Apply Migration
```bash
# Apply the migration to the database
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update'
```

### Alternative: Use Migration Script
```bash
# Use the existing migration script
./scripts/docker-migrate.sh
```

## Validation Commands

### 1. Verify Migration Files Created
```bash
# Check that migration files were generated
ls -la apps/api/Migrations/ | grep AddEncryptedOtherNames
```

### 2. Verify Database Schema
```bash
# Connect to PostgreSQL and check table structure
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T postgres psql -U postgres -d witchcityrope_dev -c "\d \"VettingApplications\""

# Check specifically for the new column
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T postgres psql -U postgres -d witchcityrope_dev -c "SELECT column_name, data_type, character_maximum_length, is_nullable FROM information_schema.columns WHERE table_name = 'VettingApplications' AND column_name IN ('EncryptedPronouns', 'EncryptedOtherNames');"
```

### 3. Verify Migration Status
```bash
# Check migration history
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list'
```

## Expected Migration SQL

The migration should generate SQL similar to:
```sql
ALTER TABLE "VettingApplications"
ADD COLUMN "EncryptedOtherNames" character varying(1000) NULL;
```

## Rollback Plan (if needed)

If you need to rollback the migration:
```bash
# Get the previous migration name
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations list'

# Rollback to previous migration (replace with actual previous migration name)
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef database update [PreviousMigrationName]'

# Remove the migration file
docker-compose -f docker-compose.yml -f docker-compose.dev.yml exec -T api bash -c 'export PATH="$PATH:/root/.dotnet/tools" && dotnet ef migrations remove'
```

## Files Modified

1. **Entity**: `/apps/api/Features/Vetting/Entities/VettingApplication.cs`
   - Added line 41: `public string? EncryptedOtherNames { get; set; }`

2. **Configuration**: `/apps/api/Features/Vetting/Entities/Configuration/VettingApplicationConfiguration.cs`
   - Added lines 39-40: Configuration for `EncryptedOtherNames` with max length 1000

## Testing After Migration

After the migration is applied, you can test that the new field is working:

```bash
# Test that the API can handle the new field
curl -X GET http://localhost:5655/health

# Check that the vetting endpoints are working
curl -X GET http://localhost:5655/api/vetting/health
```

## Verification Checklist

- [ ] Migration file generated successfully
- [ ] Migration applied to database without errors
- [ ] New column `EncryptedOtherNames` exists in `VettingApplications` table
- [ ] Column has correct type: `varchar(1000)`
- [ ] Column is nullable (allows NULL values)
- [ ] API starts without errors
- [ ] No Entity Framework configuration errors in logs

## Notes

- Both `EncryptedPronouns` and `EncryptedOtherNames` are encrypted fields for PII protection
- The fields are optional (nullable) to allow flexibility in the application form
- The 1000 character limit for `EncryptedOtherNames` accommodates multiple names/handles
- All changes follow WitchCityRope database standards and PostgreSQL optimization patterns