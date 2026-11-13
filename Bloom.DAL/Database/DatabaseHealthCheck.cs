using Bloom.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bloom.DAL.Database;

/// <summary>
/// Health check that actively probes MySQL connectivity by executing 'SELECT 1;'.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _connectionFactory.TestConnectionAsync(cancellationToken);
            if (isConnected)
            {
                return HealthCheckResult.Healthy("MySQL database connection established successfully.");
            }

            return HealthCheckResult.Unhealthy(
                "MySQL database connection test failed or credentials are not configured.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "MySQL database check encountered an exception.", ex);
        }
    }
}

