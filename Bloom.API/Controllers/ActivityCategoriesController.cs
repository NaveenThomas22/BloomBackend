using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/activity-categories")]
public class ActivityCategoriesController : BaseApiController
{
    private readonly IActivityCategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;

    public ActivityCategoriesController(
        IActivityCategoryService categoryService,
        ICurrentUserService currentUserService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ActivityCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ActivityCategoryDto>>>> GetCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var categories = await _categoryService.GetCategoriesAsync(userId, includeInactive, ct);
        return Success(categories, "Activity categories retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityCategoryDto>>> GetCategoryById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var category = await _categoryService.GetCategoryByIdAsync(userId, id, ct);
        return Success(category, "Activity category retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ActivityCategoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ActivityCategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _categoryService.CreateCustomCategoryAsync(userId, dto, ct);
        return CreatedSuccess(created, "Custom activity category created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ActivityCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActivityCategoryDto>>> UpdateCategory(
        [FromRoute] ulong id,
        [FromBody] UpdateCategoryDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _categoryService.UpdateCategoryAsync(userId, id, dto, ct);
        return Success(updated, "Activity category updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _categoryService.DeleteCategoryAsync(userId, id, ct);
        return Success(result, "Activity category deactivated or removed successfully.");
    }
}

