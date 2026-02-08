using Quater.Backend.Api.Tests.Fixtures;
using Xunit;

namespace Quater.Backend.Api.Tests;

/// <summary>
/// Collection definition for System Admin Samples tests.
/// Ensures all tests in the "SystemAdminSamples" collection share the same ApiTestFixture instance.
/// </summary>
[CollectionDefinition("SystemAdminSamples")]
public class SystemAdminSamplesCollection : ICollectionFixture<ApiTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

/// <summary>
/// Collection definition for System Admin Actions tests.
/// Ensures all tests in the "SystemAdminActions" collection share the same ApiTestFixture instance.
/// </summary>
[CollectionDefinition("SystemAdminActions")]
public class SystemAdminActionsCollection : ICollectionFixture<ApiTestFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
