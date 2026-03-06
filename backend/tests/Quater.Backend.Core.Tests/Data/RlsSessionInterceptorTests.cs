using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Data.Constants;
using Quater.Shared.Enums;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Integration tests for <see cref="Quater.Backend.Data.Interceptors.RlsSessionInterceptor"/>.
/// Verifies that the interceptor correctly sets (or skips) PostgreSQL session variables
/// (<c>app.current_lab_id</c> and <c>app.is_system_admin</c>) on every connection open.
/// </summary>
[Collection("TestDatabase")]
public class RlsSessionInterceptorTests : IAsyncLifetime
{
    private readonly TestDbContextFactoryFixture _fixture;

    public RlsSessionInterceptorTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    // ------------------------------------------------------------------
    // Helper: read a PostgreSQL session variable via current_setting().
    // The second argument 'true' makes it return NULL instead of throwing
    // when the variable has not been set.
    // ------------------------------------------------------------------
    private static async Task<string?> GetSessionVariableAsync(
        QuaterDbContext context,
        string variableName)
    {
        var result = await context.Database
            .SqlQuery<string>(
                $"SELECT current_setting({variableName}, true) AS \"Value\"")
            .FirstOrDefaultAsync();

        return result;
    }

    // ------------------------------------------------------------------
    // Test 1: Lab context → app.current_lab_id matches the lab GUID
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithLabContext_SetsCurrentLabIdSessionVariable()
    {
        // Arrange
        var labId = Guid.NewGuid();
        var accessor = new MockLabContextAccessor();
        accessor.SetContext(labId, UserRole.Technician);

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);

        // Act — open the connection by issuing any query
        await context.Database.OpenConnectionAsync();

        // Assert
        var actual = await GetSessionVariableAsync(context, RlsConstants.CurrentLabIdVariable);
        actual.Should().Be(labId.ToString(),
            because: "the interceptor should set app.current_lab_id to the lab GUID string");
    }

    // ------------------------------------------------------------------
    // Test 2: Lab context → app.is_system_admin is 'false'
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithLabContext_SetsIsSystemAdminFalse()
    {
        // Arrange
        var accessor = new MockLabContextAccessor();
        accessor.SetContext(Guid.NewGuid(), UserRole.Admin);

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);
        await context.Database.OpenConnectionAsync();

        // Assert
        var actual = await GetSessionVariableAsync(context, RlsConstants.IsSystemAdminVariable);
        actual.Should().Be("false",
            because: "a regular lab user should not have system-admin privileges");
    }

    // ------------------------------------------------------------------
    // Test 3: System admin → app.is_system_admin is 'true'
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithSystemAdminContext_SetsIsSystemAdminTrue()
    {
        // Arrange
        var accessor = new MockLabContextAccessor();
        accessor.SetSystemAdmin();

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);
        await context.Database.OpenConnectionAsync();

        // Assert
        var actual = await GetSessionVariableAsync(context, RlsConstants.IsSystemAdminVariable);
        actual.Should().Be("true",
            because: "system admins bypass RLS and is_system_admin should be 'true'");
    }

    // ------------------------------------------------------------------
    // Test 4: System admin → app.current_lab_id is empty/unset (irrelevant)
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithSystemAdminContext_SetsCurrentLabIdToEmpty()
    {
        // Arrange
        var accessor = new MockLabContextAccessor();
        accessor.SetSystemAdmin();

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);
        await context.Database.OpenConnectionAsync();

        // Assert — system admin sets lab_id to empty string
        var actual = await GetSessionVariableAsync(context, RlsConstants.CurrentLabIdVariable);
        actual.Should().Be(string.Empty,
            because: "system admin context sets current_lab_id to empty string, " +
                     "which NULLIF treats as NULL in RLS policy");
    }

    // ------------------------------------------------------------------
    // Test 5: No context → query executes without error, defaults are set
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithNoContext_SetsDefaultSessionVariables()
    {
        // Arrange — accessor with no context set at all
        var accessor = new MockLabContextAccessor();

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);

        // Act — should not throw
        var act = async () =>
        {
            await context.Database.OpenConnectionAsync();
            // Verify defaults are applied to clear any stale connection state
            var labId = await GetSessionVariableAsync(context, RlsConstants.CurrentLabIdVariable);
            var isAdmin = await GetSessionVariableAsync(context, RlsConstants.IsSystemAdminVariable);
            labId.Should().Be(string.Empty,
                because: "default current_lab_id should be empty string (treated as NULL by policies)");
            isAdmin.Should().Be("false",
                because: "default is_system_admin should be false to avoid stale session values");
        };

        await act.Should().NotThrowAsync(
            because: "the interceptor must gracefully skip when no lab context is configured");
    }

    // ------------------------------------------------------------------
    // Test 6: No context after prior context → defaults overwrite stale values
    // ------------------------------------------------------------------
    [Fact]
    public async Task ConnectionOpened_WithNoContextAfterPriorContext_ResetsSessionVariables()
    {
        // Arrange
        var accessor = new MockLabContextAccessor();
        var labId = Guid.NewGuid();
        accessor.SetContext(labId, UserRole.Technician);

        await using var context = _fixture.Factory.CreateContextWithRlsInterceptor(accessor);
        await context.Database.OpenConnectionAsync();

        var initialLabId = await GetSessionVariableAsync(context, RlsConstants.CurrentLabIdVariable);
        var initialIsAdmin = await GetSessionVariableAsync(context, RlsConstants.IsSystemAdminVariable);
        initialLabId.Should().Be(labId.ToString(), because: "lab context should set current_lab_id");
        initialIsAdmin.Should().Be("false", because: "lab context should set is_system_admin to false");

        await context.Database.CloseConnectionAsync();

        // Act — clear context and reopen to simulate pooled connection reuse
        accessor.ClearContext();
        await context.Database.OpenConnectionAsync();

        // Assert
        var labIdAfter = await GetSessionVariableAsync(context, RlsConstants.CurrentLabIdVariable);
        var isAdminAfter = await GetSessionVariableAsync(context, RlsConstants.IsSystemAdminVariable);
        labIdAfter.Should().Be(string.Empty,
            because: "default current_lab_id should clear any prior lab context");
        isAdminAfter.Should().Be("false",
            because: "default is_system_admin should overwrite any stale session value");
    }
}

/// <summary>
/// Test-only mock of <see cref="ILabContextAccessor"/> that allows state to be set directly.
/// </summary>
internal sealed class MockLabContextAccessor : ILabContextAccessor
{
    private Guid? _currentLabId;
    private UserRole? _currentRole;
    private bool _isSystemAdmin;

    public Guid? CurrentLabId => _currentLabId;
    public UserRole? CurrentRole => _currentRole;
    public bool IsSystemAdmin => _isSystemAdmin;

    public void SetContext(Guid labId, UserRole role)
    {
        _currentLabId = labId;
        _currentRole = role;
        _isSystemAdmin = false;
    }

    public void SetSystemAdmin()
    {
        _isSystemAdmin = true;
        _currentLabId = null;
        _currentRole = null;
    }

    public void ClearContext()
    {
        _isSystemAdmin = false;
        _currentLabId = null;
        _currentRole = null;
    }
}
