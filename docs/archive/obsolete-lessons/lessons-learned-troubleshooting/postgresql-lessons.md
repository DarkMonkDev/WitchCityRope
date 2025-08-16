# PostgreSQL Integration Tests Migration - January 15, 2025

## Key Learnings

1. **In-Memory Database Limitations**
   - Hides real database constraints
   - Doesn't enforce foreign keys properly
   - Allows invalid configurations
   - No timezone enforcement

2. **EF Core Limitations**
   - Cannot configure nullable owned entities with required properties
   - Requires workarounds for complex domain models
   - Strict about DateTime timezone handling with PostgreSQL

3. **Entity Design Requirements**
   - Always initialize Id in constructors
   - Use UTC for all DateTime values
   - Consider EF Core limitations when designing value objects
   - Test data must be unique for parallel execution

## Migration Guide

A comprehensive migration guide has been created at:
`/tests/WitchCityRope.IntegrationTests/MIGRATION_TO_POSTGRESQL.md`

This guide includes:
- Step-by-step migration instructions
- Common pitfalls and solutions
- Code examples for each issue
- Troubleshooting tips

## Recommendations

1. **Always Use Real Database for Integration Tests**
   - In-memory provider should only be used for unit tests
   - Real database reveals actual constraint violations
   - Better confidence in production behavior

2. **Entity Framework Core Best Practices**
   - Avoid nullable owned entities with required properties
   - Always use UTC DateTime values
   - Initialize all required fields in constructors
   - Test with real database during development

3. **Test Data Management**
   - Always use unique values (GUIDs) for test data
   - Consider test data builders for consistency
   - Clean up test data appropriately
   - Share expensive resources like database containers

### Documentation
- `/CLAUDE.md` - Added PostgreSQL migration warnings
- `/README.md` - Updated testing section
- `/docs/TESTING.md` - Comprehensive testing guide
- `/tests/WitchCityRope.IntegrationTests/README.md` - Test project documentation
- `/tests/WitchCityRope.IntegrationTests/MIGRATION_TO_POSTGRESQL.md` - Migration guide

