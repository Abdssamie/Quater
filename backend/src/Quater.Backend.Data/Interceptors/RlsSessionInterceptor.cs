using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Data.Constants;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that sets PostgreSQL session variables for Row-Level Security (RLS)
/// on every opened connection. Executes SET commands for <c>app.current_lab_id</c> and
/// <c>app.is_system_admin</c> so that RLS policies evaluate against the correct context.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor must be registered as <b>scoped</b> because it depends on
/// <see cref="ILabContextAccessor"/>, which is also scoped (per-request).
/// </para>
/// <para>
/// When no lab context is set (e.g., during migrations, seeding, or background services),
/// the interceptor applies safe defaults to avoid stale session values on pooled connections.
/// </para>
/// <para>
/// Uses <c>SET</c> (session-scoped) rather than <c>SET LOCAL</c> (transaction-scoped)
/// because EF Core does not always wrap individual queries in explicit transactions.
/// Using <c>SET LOCAL</c> outside a transaction has no effect and the variable reverts
/// to its default immediately.
/// </para>
/// </remarks>
public sealed class RlsSessionInterceptor(
    ILabContextAccessor labContextAccessor,
    ILogger<RlsSessionInterceptor>? logger = null) : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetRlsVariables(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetRlsVariablesAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    /// <summary>
    /// Synchronously executes the RLS SET commands on the given connection.
    /// Delegates to the async path via GetAwaiter().GetResult() — acceptable here because
    /// <see cref="ConnectionOpened"/> is invoked synchronously by the Npgsql driver and
    /// blocking is unavoidable in this code path.
    /// </summary>
    private void SetRlsVariables(DbConnection connection) =>
        SetRlsVariablesAsync(connection, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Asynchronously executes the RLS SET commands on the given connection.
    /// </summary>
    private async Task SetRlsVariablesAsync(DbConnection connection, CancellationToken ct)
    {
        BuildCommandValues(out var isSystemAdmin, out var labIdValue);

        try
        {
            await ExecuteSetCommandAsync(connection, RlsConstants.IsSystemAdminVariable,
                isSystemAdmin ? "true" : "false", ct);
            await ExecuteSetCommandAsync(connection, RlsConstants.CurrentLabIdVariable, labIdValue, ct);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex,
                "Failed to set RLS session variables (is_system_admin={IsSystemAdmin}, lab_id={LabId}). " +
                "RLS policies may not function correctly.",
                isSystemAdmin, labIdValue);
            throw;
        }
    }

    /// <summary>
    /// Determines what values to use for the RLS session variables.
    /// Uses safe defaults when no context is present (migrations, background services).
    /// </summary>
    private void BuildCommandValues(out bool isSystemAdmin, out string labIdValue)
    {
        isSystemAdmin = labContextAccessor.IsSystemAdmin;
        labIdValue = string.Empty;

        if (isSystemAdmin)
        {
            // System admin bypasses RLS — lab ID is irrelevant
            logger?.LogDebug("RlsSessionInterceptor: setting system admin context");
            return;
        }

        if (labContextAccessor.CurrentLabId.HasValue)
        {
            labIdValue = labContextAccessor.CurrentLabId.Value.ToString();
            logger?.LogDebug("RlsSessionInterceptor: setting lab context {LabId}", labIdValue);
            return;
        }

        // No context set — reset to safe defaults to avoid stale session values
        // This is normal during EF migrations, seeding, and unauthenticated background work
        logger?.LogDebug("RlsSessionInterceptor: no lab context — applying safe defaults");
    }

    /// <summary>
    /// Executes an asynchronous <c>set_config</c> command on the given connection.
    /// Uses parameterized <c>set_config</c> to avoid SQL injection.
    /// The sync path delegates here via <c>GetAwaiter().GetResult()</c>.
    /// </summary>
    private static async Task ExecuteSetCommandAsync(
        DbConnection connection,
        string variableName,
        string value,
        CancellationToken ct)
    {
        await using var cmd = connection.CreateCommand();
        // set_config(setting_name, new_value, is_local) — false = session scope
        cmd.CommandText = "SELECT set_config(@name, @value, false)";
        var nameParam = cmd.CreateParameter();
        nameParam.ParameterName = "@name";
        nameParam.Value = variableName;
        cmd.Parameters.Add(nameParam);

        var valueParam = cmd.CreateParameter();
        valueParam.ParameterName = "@value";
        valueParam.Value = value;
        cmd.Parameters.Add(valueParam);

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
