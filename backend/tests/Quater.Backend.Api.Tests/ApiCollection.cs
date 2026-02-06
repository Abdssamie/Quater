using Quater.Backend.Api.Tests.Fixtures;
using Xunit;

namespace Quater.Backend.Api.Tests;

/// <summary>
/// Collection definition for API integration tests.
/// Ensures all tests in the "Api" collection share the same ApiTestFixture instance.
/// </summary>
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
