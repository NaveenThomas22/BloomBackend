using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/reflections")]
public class ReflectionsController : BaseApiController
{
    private readonly IReflectionService _reflectionService;
    private readonly ICurrentUserService _currentUserService;

    public ReflectionsController(
        IReflectionService reflectionService,
        ICurrentUserService currentUserService)
    {
        _reflectionService = reflectionService ?? throw new ArgumentNullException(nameof(reflectionService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("moods")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReflectionMoodDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReflectionMoodDto>>>> GetMoods(CancellationToken ct)
    {
        var moods = await _reflectionService.GetMoodsAsync(ct);
        return Success(moods, "Reflection moods retrieved successfully.");
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<DailyReflectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyReflectionDto>>> GetReflectionByDate(
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var reflection = await _reflectionService.GetReflectionByDateAsync(userId, date, ct);
        return Success(reflection, "Daily reflection retrieved successfully.");
    }

    [HttpPost("morning")]
    [ProducesResponseType(typeof(ApiResponse<DailyReflectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyReflectionDto>>> SaveMorningReflection(
        [FromBody] SaveMorningReflectionRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var reflection = await _reflectionService.SaveMorningReflectionAsync(userId, request, ct);
        return Success(reflection, "Morning reflection saved successfully.");
    }

    [HttpPost("evening")]
    [ProducesResponseType(typeof(ApiResponse<DailyReflectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DailyReflectionDto>>> SaveEveningReflection(
        [FromBody] SaveEveningReflectionRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var reflection = await _reflectionService.SaveEveningReflectionAsync(userId, request, ct);
        return Success(reflection, "Evening reflection saved successfully.");
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<WeeklyReflectionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<WeeklyReflectionSummaryDto>>> GetWeeklySummary(
        [FromQuery] DateOnly? startOfWeek,
        [FromQuery] DateOnly? endOfWeek,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var summary = await _reflectionService.GetWeeklySummaryAsync(userId, startOfWeek, endOfWeek, ct);
        return Success(summary, "Weekly reflection summary retrieved successfully.");
    }
}

