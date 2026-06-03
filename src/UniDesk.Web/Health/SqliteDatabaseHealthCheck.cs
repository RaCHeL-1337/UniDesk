using Microsoft.Extensions.Diagnostics.HealthChecks;
using UniDesk.Web.Data;

namespace UniDesk.Web.Health;

public class SqliteDatabaseHealthCheck : IHealthCheck
{
    private readonly UniDeskDbContext _context;

    public SqliteDatabaseHealthCheck(UniDeskDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            bool canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("SQLite database is reachable.")
                : HealthCheckResult.Unhealthy("SQLite database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQLite database health check failed.", ex);
        }
    }
}
