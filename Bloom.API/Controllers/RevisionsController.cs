using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/revisions")]
public class RevisionsController : BaseApiController
{
    private readonly IRevisionScheduleService _revisionService;
    private readonly ICurrentUserService _currentUserService;

    public RevisionsController(
        IRevisionScheduleService revisionService,
        ICurrentUserService currentUserService)
    {
        _revisionService = revisionService ?? throw new ArgumentNullException(nameof(revisionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<RevisionDashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevisionDashboardSummaryDto>>> GetDashboard(
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var summary = await _revisionService.GetDashboardSummaryAsync(userId, date, ct);
        return Success(summary, "Revision dashboard summary retrieved successfully.");
    }

    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RevisionScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionScheduleDto>>>> GetUpcoming(
        [FromQuery] int limit = 30,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var upcoming = await _revisionService.GetUpcomingSchedulesAsync(userId, limit, ct);
        return Success(upcoming, "Upcoming revision schedules retrieved successfully.");
    }

    [HttpGet("calendar")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RevisionScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionScheduleDto>>>> GetCalendar(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var schedules = await _revisionService.GetSchedulesForRangeAsync(userId, startDate, endDate, ct);
        return Success(schedules, "Revision calendar schedules retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<RevisionScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RevisionScheduleDto>>> GetById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var schedule = await _revisionService.GetScheduleByIdAsync(id, userId, ct);
        if (schedule == null)
        {
            return Failure<RevisionScheduleDto>($"Revision schedule with ID {id} not found.", StatusCodes.Status404NotFound);
        }
        return Success(schedule, "Revision schedule retrieved successfully.");
    }

    [HttpGet("attempts/topic/{topicId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RevisionAttemptDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionAttemptDto>>>> GetAttemptsByTopic(
        [FromRoute] ulong topicId,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempts = await _revisionService.GetAttemptsForTopicAsync(userId, topicId, ct);
        return Success(attempts, "Revision attempts retrieved successfully.");
    }

    [HttpGet("attempts/recent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RevisionAttemptDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionAttemptDto>>>> GetRecentAttempts(
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempts = await _revisionService.GetRecentAttemptsAsync(userId, limit, ct);
        return Success(attempts, "Recent revision attempts retrieved successfully.");
    }

    public record ScheduleTopicRevisionRequest(ulong TopicId, ulong? PolicyId = null, DateOnly? FromDate = null);

    [HttpPost("schedule")]
    [ProducesResponseType(typeof(ApiResponse<RevisionScheduleDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RevisionScheduleDto>>> ScheduleRevision(
        [FromBody] ScheduleTopicRevisionRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var schedule = await _revisionService.ScheduleInitialRevisionForTopicAsync(
            userId, request.TopicId, request.PolicyId, request.FromDate, ct);
        return CreatedSuccess(schedule, "Revision scheduled successfully.");
    }

    [HttpPost("attempt")]
    [ProducesResponseType(typeof(ApiResponse<RevisionAttemptDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RevisionAttemptDto>>> RecordAttempt(
        [FromBody] RecordRevisionAttemptRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempt = await _revisionService.RecordAttemptAsync(userId, request, ct);
        return CreatedSuccess(attempt, "Revision attempt recorded successfully.");
    }

    [HttpPut("{id:long}/reschedule")]
    [ProducesResponseType(typeof(ApiResponse<RevisionScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RevisionScheduleDto>>> Reschedule(
        [FromRoute] ulong id,
        [FromBody] RescheduleRevisionRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _revisionService.RescheduleAsync(id, userId, request, ct);
        return Success(updated, "Revision rescheduled successfully.");
    }

    public record SkipRevisionRequest(string? Reason);

    [HttpPost("{id:long}/skip")]
    [ProducesResponseType(typeof(ApiResponse<RevisionScheduleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevisionScheduleDto>>> Skip(
        [FromRoute] ulong id,
        [FromBody] SkipRevisionRequest? request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _revisionService.SkipAsync(id, userId, request?.Reason, ct);
        return Success(updated, "Revision skipped successfully.");
    }
}

