using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/goals")]
public class GoalsController : BaseApiController
{
    private readonly IUserGoalService _goalService;
    private readonly ICurrentUserService _currentUserService;

    public GoalsController(
        IUserGoalService goalService,
        ICurrentUserService currentUserService)
    {
        _goalService = goalService ?? throw new ArgumentNullException(nameof(goalService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserGoalDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserGoalDto>>>> GetGoals(
        [FromQuery] string? period = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var goals = await _goalService.GetGoalsAsync(userId, period, status, ct);
        return Success(goals, "Goals retrieved successfully.");
    }

    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiResponse<GoalDashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GoalDashboardSummaryDto>>> GetDashboard(
        [FromQuery] string period = "Daily",
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var dashboard = await _goalService.GetDashboardAsync(userId, period, date, ct);
        return Success(dashboard, $"{period} goal dashboard retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserGoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserGoalDto>>> GetGoalById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var goal = await _goalService.GetGoalByIdAsync(id, userId, ct);
        return Success(goal, "Goal retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserGoalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<UserGoalDto>>> CreateGoal(
        [FromBody] CreateGoalDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _goalService.CreateGoalAsync(userId, dto, ct);
        return CreatedSuccess(created, "Goal created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserGoalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserGoalDto>>> UpdateGoal(
        [FromRoute] ulong id,
        [FromBody] UpdateGoalDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _goalService.UpdateGoalAsync(id, userId, dto, ct);
        return Success(updated, "Goal updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteGoal(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _goalService.DeleteGoalAsync(id, userId, ct);
        return Success("Goal deleted successfully.");
    }
}

