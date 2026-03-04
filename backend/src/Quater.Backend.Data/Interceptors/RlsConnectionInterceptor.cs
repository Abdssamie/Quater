using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data.Constants;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// Sets PostgreSQL RLS session variables (<c>app.current_lab_id</c>,
/// <c>app.is_system_admin</c>) on every connection EF Core opens.
/// Uses <c>SET LOCAL</c> (transaction-scoped) to prevent bleed across pooled connections.
/// No-op when neither <see cref="ILabContextAccessor.IsSystemAdmin"/> nor
/// <see cref="ILabContextAccessor.CurrentLabId"/> is set.
/// </summary>
public sealed class RlsConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ILabContextAccessor _labContext;
    private readonly ILogger<RlsConnectionInterceptor>? _logger;

    public RlsConnectionInterceptor(
        ILabContextAccessor labContext,
        ILogger<RlsConnectionInterceptor>? logger = null)
    {
        _labContext = labContext ?? throw new ArgumentNullException(nameof(labContext));
        _logger = logger;
    }

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetRlsVariables(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetRlsVariablesAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    // Internal for unit testing
    internal void ApplyRlsVariables(DbConnection connection) => SetRlsVariables(connection);
    internal Task ApplyRlsVariablesAsync(DbConnection connection, CancellationToken ct = default) => SetRlsVariablesAsync(connection, ct);

    private void SetRlsVariables(DbConnection connection)
    {
        if (_labContext.IsSystemAdmin)
        {
            ExecuteSetLocal(connection, RlsConstants.IsSystemAdminVariable, "true");
            ExecuteSetLocal(connection, RlsConstants.CurrentLabIdVariable, string.Empty);
            _logger?.LogDebug("RLS: system admin — {IsAdmin}='true'", RlsConstants.IsSystemAdminVariable);
        }
        else if (_labContext.CurrentLabId.HasValue)
        {
            var labId = _labContext.CurrentLabId.Value.ToString();
            ExecuteSetLocal(connection, RlsConstants.CurrentLabIdVariable, labId);
            ExecuteSetLocal(connection, RlsConstants.IsSystemAdminVariable, "false");
            _logger?.LogDebug("RLS: lab context — {LabId}='{LabIdValue}'", RlsConstants.CurrentLabIdVariable, labId);
        }
    }

    private async Task SetRlsVariablesAsync(DbConnection connection, CancellationToken ct)
    {
        if (_labContext.IsSystemAdmin)
        {
            await ExecuteSetLocalAsync(connection, RlsConstants.IsSystemAdminVariable, "true", ct);
            await ExecuteSetLocalAsync(connection, RlsConstants.CurrentLabIdVariable, string.Empty, ct);
            _logger?.LogDebug("RLS: system admin — {IsAdmin}='true'", RlsConstants.IsSystemAdminVariable);
        }
        else if (_labContext.CurrentLabId.HasValue)
        {
            var labId = _labContext.CurrentLabId.Value.ToString();
            await ExecuteSetLocalAsync(connection, RlsConstants.CurrentLabIdVariable, labId, ct);
            await ExecuteSetLocalAsync(connection, RlsConstants.IsSystemAdminVariable, "false", ct);
            _logger?.LogDebug("RLS: lab context — {LabId}='{LabIdValue}'", RlsConstants.CurrentLabIdVariable, labId);
        }
    }

    private static void ExecuteSetLocal(DbConnection connection, string variable, string value)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetLocalSql(variable, value);
        cmd.ExecuteNonQuery();
    }

    private static async Task ExecuteSetLocalAsync(DbConnection connection, string variable, string value, CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = BuildSetLocalSql(variable, value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string BuildSetLocalSql(string variable, string value)
    {
        // Strip single quotes defensively — values are UUIDs, "true", "false", or empty.
        var safeValue = value.Replace("'", string.Empty, StringComparison.Ordinal);
        return $"SET LOCAL \"{variable}\" = '{safeValue}'";
    }
}
