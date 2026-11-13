using System.Diagnostics;
using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;

namespace Bloom.BLL.Services;

public class HealthService : IHealthService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HealthService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public Task<HealthStatusDto> GetApplicationHealthAsync(CancellationToken cancellationToken = default)
    {
        var result = new HealthStatusDto
        {
            Status = "Healthy",
            Application = "Bloom - My CAT Journey",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            TimestampUtc = DateTime.UtcNow
        };

        return Task.FromResult(result);
    }

    public async Task<DatabaseHealthDto> GetDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var isConnected = await _connectionFactory.TestConnectionAsync(cancellationToken);
        stopwatch.Stop();

        return new DatabaseHealthDto
        {
            Status = isConnected ? "Healthy" : "Unhealthy",
            IsConnected = isConnected,
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            Message = isConnected 
                ? "MySQL connection verified successfully." 
                : "MySQL database is unreachable or credentials are not yet configured.",
            CheckedAtUtc = DateTime.UtcNow
        };
    }
}

