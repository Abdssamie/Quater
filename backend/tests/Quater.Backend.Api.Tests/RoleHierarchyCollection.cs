using Quater.Backend.Api.Tests.Fixtures;
using Xunit;

namespace Quater.Backend.Api.Tests;

/// <summary>
/// Collection definition for Role Hierarchy tests.
/// Ensures all tests in the "RoleHierarchy" collection share the same ApiTestFixture instance.
/// </summary>
[CollectionDefinition("RoleHierarchy")]
public class RoleHierarchyCollection : ICollectionFixture<ApiTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
