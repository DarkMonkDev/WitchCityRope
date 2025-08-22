# Build Errors Documentation

## Summary
The SQLite database has been successfully created with all required tables through Entity Framework Core migrations. However, the Api project has several build errors that need to be addressed.

## Database Creation Status
✅ **Database created successfully**
- Location: `/src/WitchCityRope.Api/witchcityrope.db`
- Migration: `InitialCreate` (20250627012409)
- Tables created:
  - Users
  - Events
  - Registrations
  - Payments
  - VettingApplications
  - IncidentReports

## Migration Commands Used
1. `dotnet tool restore` - Restored EF Core tools
2. Created temporary MigrationRunner project to bypass Api build errors
3. `dotnet ef migrations add InitialCreate -s src/WitchCityRope.MigrationRunner -p src/WitchCityRope.Infrastructure`
4. `dotnet ef database update -s src/WitchCityRope.MigrationRunner -p src/WitchCityRope.Infrastructure`

## Key Build Errors in Api Project

### 1. EventsController Issues
- Missing `organizerId` parameter in `CreateEventAsync` call
- Missing methods: `ListEventsAsync`, `GetFeaturedEventsAsync`, `RegisterForEventAsync`
- `IEventService` interface not found

### 2. AuthController Issues
- Ambiguous reference to `UnauthorizedException` between:
  - `WitchCityRope.Api.Exceptions.UnauthorizedException`
  - `WitchCityRope.Api.Features.Auth.Services.UnauthorizedException`

### 3. AuthService Issues
- Constructor issues with domain entities:
  - `User` constructor requires `encryptedLegalName` parameter
  - `EmailAddress` constructor signature mismatch
  - `SceneName` constructor signature mismatch

### 4. EventService Issues
- EventDto properties not matching expected structure
- Event entity properties are read-only (need to use proper methods/constructors)

### 5. Service Registration Issues
- `IEventService` interface not properly defined or imported

## Next Steps
1. Fix the interface definitions and implementations
2. Resolve ambiguous references by using fully qualified names or aliases
3. Update constructors to match domain entity requirements
4. Ensure DTOs match the expected structure
5. Update service methods to use proper entity construction patterns

## Migration Notes
- Migrations are now located in: `/src/WitchCityRope.Infrastructure/Migrations/`
- To run future migrations after fixing build errors:
  ```bash
  dotnet ef migrations add [MigrationName] -s src/WitchCityRope.Api -p src/WitchCityRope.Infrastructure
  dotnet ef database update -s src/WitchCityRope.Api -p src/WitchCityRope.Infrastructure
  ```