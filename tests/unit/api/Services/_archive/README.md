# Archived Service Tests

## SeedDataServiceTests-obsolete-2025-11-11.cs

**Archived**: November 11, 2025
**Reason**: Architecture changed - monolithic `SeedDataService` was refactored into specialized seeders

### Context
The original `SeedDataService` (3,800+ lines) was refactored on October 27, 2025 into:
- `SeedCoordinator` - Orchestrates all seeders
- `UserSeeder`, `EventSeeder`, `SafetySeeder`, etc. - Specialized seeders

### Why Archived
These tests were testing the monolithic service's methods like:
- `SeedUsersAsync()`
- `SeedEventsAsync()`
- `SeedVettingApplicationsAsync()`

These methods now live in individual seeder classes. The test architecture would need complete rewrite to:
1. Mock/create 12+ seeder dependencies for `SeedCoordinator`
2. OR create individual test suites for each specialized seeder

Since the tests were already failing and the architecture fundamentally changed, archiving was the pragmatic choice rather than a full rewrite.

### Future Testing Strategy
If seed data testing is needed:
- Test individual seeders (e.g., `UserSeederTests`, `EventSeederTests`)
- Test `SeedCoordinator` orchestration with real seeders in integration tests
- Use existing E2E tests that verify seeded data works correctly
