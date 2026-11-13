using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

/// <summary>
/// Probes application vitality and database connectivity.
/// </summary>
public class HealthController : BaseApiController
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService ?? throw new ArgumentNullException(nameof(healthService));
    }

    /// <summary>
    /// Checks basic application API vitality.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HealthStatusDto>>> GetHealth(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetApplicationHealthAsync(cancellationToken);
        return Success(health, "Bloom API is online and operational.");
    }

    /// <summary>
    /// Actively probes MySQL database connectivity via asynchronous test query.
    /// </summary>
    [HttpGet("database")]
    [ProducesResponseType(typeof(ApiResponse<DatabaseHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DatabaseHealthDto>), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ApiResponse<DatabaseHealthDto>>> GetDatabaseHealth(CancellationToken cancellationToken)
    {
        var dbHealth = await _healthService.GetDatabaseHealthAsync(cancellationToken);
        
        if (dbHealth.IsConnected)
        {
            return Success(dbHealth, "Database is connected and operational.");
        }

        // Return 503 with structured envelope when database is unreachable or credentials are not configured
        return StatusCode(StatusCodes.Status503ServiceUnavailable, 
            ApiResponse<DatabaseHealthDto>.Fail(dbHealth.Message, StatusCodes.Status503ServiceUnavailable));
    }
}

