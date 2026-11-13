using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/bookmarks")]
public class BookmarksController : BaseApiController
{
    private readonly IBookmarkService _bookmarkService;
    private readonly ICurrentUserService _currentUserService;

    public BookmarksController(IBookmarkService bookmarkService, ICurrentUserService currentUserService)
    {
        _bookmarkService = bookmarkService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QuestionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<QuestionDto>>>> GetBookmarks(
        [FromQuery] ulong? subjectId,
        [FromQuery] ulong? topicId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var (questions, total) = await _bookmarkService.GetBookmarksAsync(userId, subjectId, topicId, page, pageSize, ct);
        return Success(new PagedResult<QuestionDto>(questions, page, pageSize, total), "Bookmarks retrieved successfully.");
    }

    [HttpPost("{questionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> AddBookmark(
        [FromRoute] ulong questionId,
        [FromBody] AddBookmarkRequest? request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var ok = await _bookmarkService.AddBookmarkAsync(userId, questionId, request ?? new AddBookmarkRequest(), ct);
        return Success(ok, "Question bookmarked successfully.");
    }

    [HttpDelete("{questionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveBookmark(
        [FromRoute] ulong questionId,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var ok = await _bookmarkService.RemoveBookmarkAsync(userId, questionId, ct);
        return Success(ok, "Bookmark removed successfully.");
    }
}
