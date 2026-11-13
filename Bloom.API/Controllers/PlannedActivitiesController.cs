using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/planned-activities")]
public class PlannedActivitiesController : BaseApiController
{
    private readonly IPlannedActivityService _plannedService;
    private readonly ICurrentUserService _currentUserService;

    public PlannedActivitiesController(
        IPlannedActivityService plannedService,
        ICurrentUserService currentUserService)
    {
        _plannedService = plannedService ?? throw new ArgumentNullException(nameof(plannedService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlannedActivityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PlannedActivityDto>>>> GetPlannedActivities(
        [FromQuery] DateOnly? targetDate = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await _plannedService.GetPlannedActivitiesAsync(userId, date, timezoneId, ct);
        return Success(items, "Planned activities retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PlannedActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlannedActivityDto>>> GetById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var item = await _plannedService.GetPlannedByIdAsync(userId, id, ct);
        return Success(item, "Planned activity retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PlannedActivityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PlannedActivityDto>>> Create(
        [FromBody] CreatePlannedActivityDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _plannedService.CreatePlannedActivityAsync(userId, dto, ct);
        return CreatedSuccess(created, "Planned activity scheduled successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PlannedActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlannedActivityDto>>> Update(
        [FromRoute] ulong id,
        [FromBody] UpdatePlannedActivityDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _plannedService.UpdatePlannedActivityAsync(userId, id, dto, ct);
        return Success(updated, "Planned activity updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _plannedService.DeletePlannedActivityAsync(userId, id, ct);
        return Success(result, "Planned activity deleted successfully.");
    }

    [HttpPost("{id:long}/start")]
    [ProducesResponseType(typeof(ApiResponse<ActivityRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityRecordDto>>> StartPlan(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var record = await _plannedService.StartPlannedActivityAsync(userId, id, ct);
        return Success(record, "Planned activity started.");
    }

    [HttpPost("{id:long}/skip")]
    [ProducesResponseType(typeof(ApiResponse<PlannedActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PlannedActivityDto>>> SkipPlan(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var plan = await _plannedService.SkipPlannedActivityAsync(userId, id, ct);
        return Success(plan, "Planned activity marked as skipped.");
    }
}

