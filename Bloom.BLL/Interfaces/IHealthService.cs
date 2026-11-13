using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IHealthService
{
    Task<HealthStatusDto> GetApplicationHealthAsync(CancellationToken cancellationToken = default);
    Task<DatabaseHealthDto> GetDatabaseHealthAsync(CancellationToken cancellationToken = default);
}

