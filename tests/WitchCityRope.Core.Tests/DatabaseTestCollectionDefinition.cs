using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.Core.Tests;

/// <summary>
/// Test collection definition for PostgreSQL integration tests in Core.Tests
/// This ensures tests share the same container instance for performance
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseTestCollectionDefinition : ICollectionFixture<DatabaseTestFixture>
{
    // This class has no code, and is never created. Its purpose is just
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}