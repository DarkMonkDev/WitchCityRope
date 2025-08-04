using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Collection definition for tests that use PostgreSQL container
/// This ensures that tests in the same collection run sequentially and share the same database container
/// </summary>
[CollectionDefinition("PostgreSQL Integration Tests")]
public class PostgreSqlTestCollection : ICollectionFixture<PostgreSqlFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}