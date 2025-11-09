using WitchCityRope.Tests.Common.Fixtures;
using Xunit;

namespace WitchCityRope.IntegrationTests;

/// <summary>
/// Test collection definition for tests that must run sequentially (not in parallel)
/// to avoid database conflicts and deadlocks.
///
/// Tests using [Collection("Sequential")] will:
/// - Run sequentially, not in parallel
/// - Share the same DatabaseTestFixture instance
/// - Avoid database deadlocks from parallel test execution
/// </summary>
[CollectionDefinition("Sequential", DisableParallelization = true)]
public class SequentialTestCollectionDefinition : ICollectionFixture<DatabaseTestFixture>
{
    // This class has no code, and is never created. Its purpose is just
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
    //
    // DisableParallelization = true ensures tests in this collection
    // run one at a time, preventing database deadlocks.
}
