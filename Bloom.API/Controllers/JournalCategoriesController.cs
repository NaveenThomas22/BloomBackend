using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/journal-categories")]
public class JournalCategoriesController : BaseApiController
{
    private readonly IJournalService _journalService;
    private readonly ICurrentUserService _currentUserService;

    public JournalCategoriesController(
        IJournalService journalService,
        ICurrentUserService currentUserService)
    {
        _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<JournalCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JournalCategoryDto>>>> GetCategories(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var categories = await _journalService.GetCategoriesAsync(userId, ct);
        return Success(categories, "Journal categories retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<JournalCategoryDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<JournalCategoryDto>>> CreateCategory(
        [FromBody] CreateJournalCategoryRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var category = await _journalService.CreateUserCategoryAsync(userId, request, ct);
        return CreatedSuccess(category, "Custom journal category created successfully.");
    }
}

