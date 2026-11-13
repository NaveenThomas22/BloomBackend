using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/questions")]
public class QuestionsController : BaseApiController
{
    private readonly IQuestionService _questionService;
    private readonly ICurrentUserService _currentUserService;

    public QuestionsController(IQuestionService questionService, ICurrentUserService currentUserService)
    {
        _questionService = questionService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<QuestionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<QuestionDto>>>> GetQuestions(
        [FromQuery] ulong? subjectId,
        [FromQuery] ulong? moduleId,
        [FromQuery] ulong? topicId,
        [FromQuery] ulong? subtopicId,
        [FromQuery] string? questionType,
        [FromQuery] string? difficultyLevel,
        [FromQuery] string? status = "Published",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var filter = new QuestionFilter(subjectId, moduleId, topicId, subtopicId, questionType, difficultyLevel, status, search);
        var (questions, totalCount) = await _questionService.GetQuestionsPagedAsync(filter, userId, page, pageSize, ct);

        return Success(new PagedResult<QuestionDto>(questions, page, pageSize, totalCount), "Questions retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> GetQuestion(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var question = await _questionService.GetQuestionByIdAsync(id, userId, false, ct);
        if (question == null) return Failure<QuestionDto>($"Question {id} not found.", StatusCodes.Status404NotFound);
        return Success(question);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion(
        [FromBody] CreateQuestionRequest request,
        CancellationToken ct = default)
    {
        var created = await _questionService.CreateQuestionAsync(request, ct);
        return CreatedSuccess(created, "Question created successfully.");
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QuestionDto>>> UpdateQuestion(
        [FromRoute] ulong id,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken ct = default)
    {
        var updated = await _questionService.UpdateQuestionAsync(id, request, ct);
        return Success(updated, "Question updated successfully.");
    }

    [HttpPost("{id:long}/publish")]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> PublishQuestion([FromRoute] ulong id, CancellationToken ct = default)
    {
        var ok = await _questionService.PublishQuestionAsync(id, ct);
        return Success(ok, "Question published successfully.");
    }

    [HttpPost("{id:long}/archive")]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> ArchiveQuestion([FromRoute] ulong id, CancellationToken ct = default)
    {
        var ok = await _questionService.ArchiveQuestionAsync(id, ct);
        return Success(ok, "Question archived successfully.");
    }
}
