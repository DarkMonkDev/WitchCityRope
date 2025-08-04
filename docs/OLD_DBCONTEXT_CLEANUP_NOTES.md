# Old DbContext Cleanup Notes

## Files Renamed to Prevent Accidental Usage

The following files have been renamed with a `.old` extension to prevent accidental usage of the old `WitchCityRopeDbContext`:

1. `/src/WitchCityRope.Infrastructure/Data/WitchCityRopeDbContext.cs` → `WitchCityRopeDbContext.cs.old`
2. `/src/WitchCityRope.Infrastructure/DesignTimeDbContextFactory.cs` → `DesignTimeDbContextFactory.cs.old`
3. `/src/WitchCityRope.Infrastructure/Migrations/WitchCityRopeDbContextModelSnapshot.cs` → `WitchCityRopeDbContextModelSnapshot.cs.old`
4. `/src/WitchCityRope.Infrastructure/Migrations/20250630020754_InitialPostgreSQLMigration.Designer.cs` → `20250630020754_InitialPostgreSQLMigration.Designer.cs.old`
5. `/src/WitchCityRope.Infrastructure/Migrations/20250701233917_MakeDecisionNotesNullable.Designer.cs` → `20250701233917_MakeDecisionNotesNullable.Designer.cs.old`

## Files That Still Reference WitchCityRopeDbContext

The following files still contain references to the old `WitchCityRopeDbContext` and need to be updated to use `WitchCityRopeIdentityDbContext`:

### Core Application Files
1. **`/src/WitchCityRope.Web/Program.cs`** - Line 42: Registers the old DbContext
   - Change: `builder.Services.AddDbContext<WitchCityRopeDbContext>` to `builder.Services.AddDbContext<WitchCityRopeIdentityDbContext>`
   - Line 265: Uses the old context for database operations

2. **`/src/WitchCityRope.Infrastructure/Data/DbInitializer.cs`** - All methods use the old context
   - Needs to be updated to accept and use `WitchCityRopeIdentityDbContext`

3. **`/src/WitchCityRope.Infrastructure/Identity/IdentityMigrationHelper.cs`** - Line 42: Gets the old context
   - This file might be part of the migration process and may need special handling

### Test Files
4. **`/tests/WitchCityRope.Api.Tests/Services/ConcurrencyAndEdgeCaseTests.cs`**
5. **`/tests/WitchCityRope.Api.Tests/Services/ConcurrencyAndEdgeCaseTests_Fixed.cs`**
6. **`/tests/WitchCityRope.Api.Tests/Helpers/MockHelpers.cs`**
   - All test files need to be updated to use the new context

### Migration Files (Already renamed but listed for completeness)
7. **`/src/WitchCityRope.Infrastructure/Migrations/20250630020754_InitialPostgreSQLMigration.cs`**
8. **`/src/WitchCityRope.Infrastructure/Migrations/20250701233917_MakeDecisionNotesNullable.cs`**
   - These old migration files may need to be removed or archived

## Key Differences Between Old and New DbContext

1. **Old DbContext (`WitchCityRopeDbContext`)**:
   - Extends `DbContext`
   - Uses `Core.Entities.User` entity
   - Has `DbSet<User> Users`

2. **New DbContext (`WitchCityRopeIdentityDbContext`)**:
   - Extends `IdentityDbContext<WitchCityRopeUser, WitchCityRopeRole, Guid>`
   - Explicitly ignores `Core.Entities.User`
   - Uses ASP.NET Core Identity entities
   - Has additional entities like `Rsvp`, `Ticket`, `UserNote`, etc.

## Recommended Next Steps

1. Update `Program.cs` to register `WitchCityRopeIdentityDbContext`
2. Update `DbInitializer.cs` to use `WitchCityRopeIdentityDbContext`
3. Update all test files to use the new context
4. Consider creating a new `DesignTimeDbContextFactory` for the Identity context if needed
5. After all references are updated and tested, the `.old` files can be safely deleted
6. Consider archiving or removing old migration files that are no longer relevant

## Important Notes

- The build will now fail due to the renamed files, which is intentional to prevent accidental usage
- Do not delete the `.old` files until all references have been successfully migrated
- The `IdentityMigrationHelper.cs` may be a special case if it's designed to migrate data from the old context to the new one