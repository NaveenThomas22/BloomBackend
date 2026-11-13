using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/habits")]
public class HabitsController : BaseApiController
{
    private readonly IHabitService _habitService;
    private readonly ICurrentUserService _currentUserService;

    public HabitsController(
        IHabitService habitService,
        ICurrentUserService currentUserService)
    {
        _habitService = habitService ?? throw new ArgumentNullException(nameof(habitService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HabitCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HabitCategoryDto>>>> GetCategories(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var categories = await _habitService.GetCategoriesAsync(userId, ct);
        return Success(categories, "Habit categories retrieved successfully.");
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<HabitOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HabitOverviewDto>>> GetOverview(
        [FromQuery] DateOnly? referenceDate,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var overview = await _habitService.GetOverviewAsync(userId, referenceDate, ct);
        return Success(overview, "Habit overview retrieved successfully.");
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UserHabitDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserHabitDto>>>> GetHabits(
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habits = await _habitService.GetHabitsAsync(userId, activeOnly, ct);
        return Success(habits, "Habits retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserHabitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserHabitDto>>> GetHabitById(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habit = await _habitService.GetHabitByIdAsync(id, userId, ct);
        if (habit == null)
        {
            return Failure<UserHabitDto>($"Habit with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return Success(habit, "Habit retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UserHabitDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UserHabitDto>>> CreateHabit(
        [FromBody] CreateHabitRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habit = await _habitService.CreateHabitAsync(userId, request, ct);
        return CreatedSuccess(habit, "Habit created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<UserHabitDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<UserHabitDto>>> UpdateHabit(
        [FromRoute] ulong id,
        [FromBody] UpdateHabitRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var habit = await _habitService.UpdateHabitAsync(id, userId, request, ct);
        return Success(habit!, "Habit updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteHabit(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _habitService.DeleteHabitAsync(id, userId, ct);
        return Success(success, "Habit deleted successfully.");
    }

    [HttpGet("completions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HabitCompletionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HabitCompletionDto>>>> GetCompletionsForDate(
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var completions = await _habitService.GetCompletionsForDateAsync(userId, date, ct);
        return Success(completions, "Habit completions retrieved successfully.");
    }

    [HttpPost("{id:long}/completions")]
    [ProducesResponseType(typeof(ApiResponse<HabitCompletionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HabitCompletionDto>>> RecordCompletion(
        [FromRoute] ulong id,
        [FromBody] RecordHabitCompletionRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var completion = await _habitService.RecordCompletionAsync(userId, id, request, ct);
        return Success(completion, "Habit completion recorded successfully.");
    }

    [HttpDelete("{id:long}/completions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCompletion(
        [FromRoute] ulong id,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _habitService.DeleteCompletionAsync(userId, id, date, ct);
        return Success(success, "Habit completion deleted successfully.");
    }
}
