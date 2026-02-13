using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Quater.Backend.Core.Interfaces;

namespace Quater.Backend.Data.Interceptors;

/// <summary>
/// EF Core interceptor that sets PostgreSQL session variables for Row Level Security (RLS)
/// on every connection open. This ensures RLS policies receive the correct lab context
/// regardless of connection pooling behavior.
/// <para>
/// Sets two session variables:
/// <list type="bullet">
///   <item><c>app.current_lab_id</c> — the lab ID for the current request (used by lab_isolation_policy)</item>
///   <item><c>app.is_system_admin</c> — "true" when the user is a system admin (bypasses RLS)</item>
/// </list>
/// </para>
/// </summary>
public class RlsSessionInterceptor : DbConnectionInterceptor
{
    private readonly ILabContextAccessor _labContext;
    private readonly ILogger<RlsSessionInterceptor> _logger;

    public RlsSessionInterceptor(ILabContextAccessor labContext, ILogger<RlsSessionInterceptor> logger)
    {
        _labContext = labContext ?? throw new ArgumentNullException(nameof(labContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSessionVariables(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetSessionVariablesAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private void SetSessionVariables(DbConnection connection)
    {
        if (_labContext.IsSystemAdmin)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.is_system_admin', 'true', true)";
            cmd.ExecuteNonQuery();

            _logger.LogDebug("RLS session: set app.is_system_admin=true");
            return;
        }

        if (_labContext.CurrentLabId.HasValue)
        {
            using var cmd = connection.CreateCommand();
            // Use positional parameter $1 for PostgreSQL to avoid any SQL injection risks
            cmd.CommandText = "SELECT set_config('app.current_lab_id', $1, true)";

            var param = cmd.CreateParameter();
            param.ParameterName = "labId";
            param.Value = _labContext.CurrentLabId.Value.ToString();
            cmd.Parameters.Add(param);

            cmd.ExecuteNonQuery();

            _logger.LogDebug("RLS session: set app.current_lab_id={LabId}", _labContext.CurrentLabId.Value);
        }
    }

    private async Task SetSessionVariablesAsync(DbConnection connection, CancellationToken ct)
    {
        if (_labContext.IsSystemAdmin)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.is_system_admin', 'true', true)";
            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogDebug("RLS session: set app.is_system_admin=true");
            return;
        }

        if (_labContext.CurrentLabId.HasValue)
        {
            await using var cmd = connection.CreateCommand();
            // Use positional parameter $1 for PostgreSQL to avoid any SQL injection risks
            cmd.CommandText = "SELECT set_config('app.current_lab_id', $1, true)";

            var param = cmd.CreateParameter();
            param.ParameterName = "labId";
            param.Value = _labContext.CurrentLabId.Value.ToString();
            cmd.Parameters.Add(param);

            await cmd.ExecuteNonQueryAsync(ct);

            _logger.LogDebug("RLS session: set app.current_lab_id={LabId}", _labContext.CurrentLabId.Value);
        }
    }
}
