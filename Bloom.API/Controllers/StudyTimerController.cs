using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/study-timer")]
public class StudyTimerController : BaseApiController
{
    private readonly IStudyTimerService _timerService;
    private readonly ICurrentUserService _currentUserService;

    public StudyTimerController(
        IStudyTimerService timerService,
        ICurrentUserService currentUserService)
    {
        _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<ActiveTimerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ActiveTimerDto>>> GetActiveTimer(CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var timer = await _timerService.GetActiveTimerAsync(userId, ct);
        return Success(timer, "Active timer state retrieved successfully.");
    }

    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<ActiveTimerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ActiveTimerDto>>> StartTimer(
        [FromBody] StartTimerDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var timer = await _timerService.StartTimerAsync(userId, dto, ct);
        return CreatedSuccess(timer, "Study timer started successfully.");
    }

    [HttpPost("pause")]
    [ProducesResponseType(typeof(ApiResponse<ActiveTimerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActiveTimerDto>>> PauseTimer(CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var timer = await _timerService.PauseTimerAsync(userId, ct);
        return Success(timer, "Study timer paused successfully.");
    }

    [HttpPost("resume")]
    [ProducesResponseType(typeof(ApiResponse<ActiveTimerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActiveTimerDto>>> ResumeTimer(CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var timer = await _timerService.ResumeTimerAsync(userId, ct);
        return Success(timer, "Study timer resumed successfully.");
    }

    [HttpPost("complete")]
    [ProducesResponseType(typeof(ApiResponse<ActivityRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityRecordDto>>> CompleteTimer(
        [FromBody] CompleteTimerDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var record = await _timerService.CompleteTimerAsync(userId, dto, ct);
        return Success(record, "Study session completed and logged successfully.");
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> CancelTimer(CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _timerService.CancelTimerAsync(userId, ct);
        return Success(result, "Active study timer cancelled.");
    }
}

