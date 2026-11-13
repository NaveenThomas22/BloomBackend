using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/practice-sessions")]
public class PracticeSessionsController : BaseApiController
{
    private readonly IPracticeSessionService _practiceService;
    private readonly ICurrentUserService _currentUserService;

    public PracticeSessionsController(IPracticeSessionService practiceService, ICurrentUserService currentUserService)
    {
        _practiceService = practiceService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PracticeSessionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PracticeSessionDto>>> StartSession(
        [FromBody] StartPracticeSessionRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var session = await _practiceService.StartSessionAsync(userId, request, ct);
        return CreatedSuccess(session, "Practice session started successfully.");
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PracticeSessionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PracticeSessionDto>>>> GetRecentSessions(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var list = await _practiceService.GetRecentSessionsAsync(userId, limit, ct);
        return Success(list);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PracticeSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PracticeSessionDto>>> GetSessionById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var session = await _practiceService.GetSessionByIdAsync(id, userId, ct);
        if (session == null) return Failure<PracticeSessionDto>($"Practice session {id} not found.", StatusCodes.Status404NotFound);
        return Success(session);
    }

    [HttpPost("{id:long}/answers")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> SaveAnswer(
        [FromRoute] ulong id,
        [FromBody] SavePracticeAnswerRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var ok = await _practiceService.SaveAnswerAsync(id, userId, request, ct);
        return Success(ok, "Answer saved successfully.");
    }

    [HttpPost("{id:long}/submit")]
    [ProducesResponseType(typeof(ApiResponse<SubmitPracticeSessionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SubmitPracticeSessionResponse>>> SubmitSession(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _practiceService.SubmitSessionAsync(id, userId, ct);
        return Success(result, "Practice session submitted successfully.");
    }
}

