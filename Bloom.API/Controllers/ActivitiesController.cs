using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/activities")]
public class ActivitiesController : BaseApiController
{
    private readonly IActivityRecordService _activityRecordService;
    private readonly ITimeAccountingService _timeAccountingService;
    private readonly ICurrentUserService _currentUserService;

    public ActivitiesController(
        IActivityRecordService activityRecordService,
        ITimeAccountingService timeAccountingService,
        ICurrentUserService currentUserService)
    {
        _activityRecordService = activityRecordService ?? throw new ArgumentNullException(nameof(activityRecordService));
        _timeAccountingService = timeAccountingService ?? throw new ArgumentNullException(nameof(timeAccountingService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("timeline")]
    [ProducesResponseType(typeof(ApiResponse<DailyTimelineDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyTimelineDto>>> GetDailyTimeline(
        [FromQuery] DateOnly? targetDate = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var timeline = await _timeAccountingService.GetDailyTimelineAsync(userId, date, timezoneId, ct);
        return Success(timeline, "Daily activity timeline retrieved successfully.");
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DailyTimeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyTimeSummaryDto>>> GetDailySummary(
        [FromQuery] DateOnly? targetDate = null,
        [FromQuery] string? timezoneId = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var summary = await _timeAccountingService.GetDailySummaryAsync(userId, date, timezoneId, ct);
        return Success(summary, "Daily time accounting summary retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityRecordDto>>> GetRecordById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var record = await _activityRecordService.GetRecordByIdAsync(userId, id, ct);
        return Success(record, "Activity record retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ActivityRecordDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityRecordDto>>> CreateRecord(
        [FromBody] CreateActivityRecordDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _activityRecordService.CreateRecordAsync(userId, dto, ct);
        return CreatedSuccess(created, "Activity logged successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityRecordDto>>> UpdateRecord(
        [FromRoute] ulong id,
        [FromBody] UpdateActivityRecordDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _activityRecordService.UpdateRecordAsync(userId, id, dto, ct);
        return Success(updated, "Activity updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteRecord(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _activityRecordService.DeleteRecordAsync(userId, id, ct);
        return Success(result, "Activity deleted successfully.");
    }
}

