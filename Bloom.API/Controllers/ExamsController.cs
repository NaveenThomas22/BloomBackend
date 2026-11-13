using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/exams")]
public class ExamsController : BaseApiController
{
    private readonly IExamService _examService;
    private readonly ICurrentUserService _currentUserService;

    public ExamsController(IExamService examService, ICurrentUserService currentUserService)
    {
        _examService = examService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExamTemplateDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExamTemplateDto>>>> GetTemplates(
        [FromQuery] string? examType = null, CancellationToken ct = default)
    {
        var list = await _examService.GetExamTemplatesAsync(examType, ct);
        return Success(list, "Exam templates retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [HttpGet("templates/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ExamTemplateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExamTemplateDto>>> GetTemplateById(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var t = await _examService.GetExamTemplateByIdAsync(id, ct);
        if (t == null) return Failure<ExamTemplateDto>($"Exam template {id} not found.", StatusCodes.Status404NotFound);
        return Success(t);
    }

    [HttpPost("{id:long}/attempts")]
    [HttpPost("templates/{id:long}/start")]
    [ProducesResponseType(typeof(ApiResponse<ActiveExamAttemptDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<ActiveExamAttemptDto>>> StartAttempt(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempt = await _examService.StartExamAttemptAsync(userId, id, ct);
        return CreatedSuccess(attempt, "Exam attempt started successfully.");
    }

    [HttpGet("attempts/active")]
    [ProducesResponseType(typeof(ApiResponse<ActiveExamAttemptDto?>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ActiveExamAttemptDto?>>> GetActiveAttempt(CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempt = await _examService.GetActiveExamAttemptAsync(userId, ct);
        return Success(attempt, attempt != null ? "Active exam attempt found." : "No active exam attempt.");
    }

    [HttpGet("attempts/recent")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ExamAttempt>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ExamAttempt>>>> GetRecentAttempts(
        [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var list = await _examService.GetRecentExamAttemptsAsync(userId, limit, ct);
        return Success(list, "Recent exam attempts retrieved successfully.");
    }

    [HttpGet("attempts/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ActiveExamAttemptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ActiveExamAttemptDto>>> GetAttemptById(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var attempt = await _examService.GetExamAttemptByIdAsync(id, userId, ct);
        if (attempt == null) return Failure<ActiveExamAttemptDto>($"Exam attempt {id} not found.", StatusCodes.Status404NotFound);
        return Success(attempt);
    }

    [HttpPost("attempts/{id:long}/answers")]
    [ProducesResponseType(typeof(ApiResponse<SaveExamAnswerResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SaveExamAnswerResponseDto>>> SaveAnswer(
        [FromRoute] ulong id,
        [FromBody] SaveExamAnswerRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var res = await _examService.SaveExamAnswerAsync(id, userId, request, ct);
        return Success(res, "Exam answer saved successfully.");
    }

    [HttpPost("attempts/{id:long}/navigate")]
    [ProducesResponseType(typeof(ApiResponse<ActiveExamAttemptDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ActiveExamAttemptDto>>> TransitionSection(
        [FromRoute] ulong id,
        [FromBody] TransitionSectionRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var res = await _examService.TransitionSectionAsync(id, userId, request, ct);
        return Success(res, "Transitioned to next exam section successfully.");
    }

    [HttpPost("attempts/{id:long}/submit")]
    [ProducesResponseType(typeof(ApiResponse<ExamResultDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ExamResultDto>>> SubmitAttempt(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _examService.SubmitExamAttemptAsync(id, userId, isAutoSubmit: false, ct);
        return Success(result, "Exam submitted successfully.");
    }

    [HttpGet("attempts/{id:long}/results")]
    [ProducesResponseType(typeof(ApiResponse<ExamResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExamResultDto>>> GetResults(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _examService.GetExamResultAsync(id, userId, ct);
        if (result == null) return Failure<ExamResultDto>($"Results for exam attempt {id} not found or test is still in progress.", StatusCodes.Status404NotFound);
        return Success(result);
    }

    [HttpGet("attempts/{id:long}/review")]
    [ProducesResponseType(typeof(ApiResponse<ExamReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExamReviewDto>>> GetReview(
        [FromRoute] ulong id, CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var review = await _examService.GetExamReviewAsync(id, userId, ct);
        if (review == null) return Failure<ExamReviewDto>($"Review for exam attempt {id} not available.", StatusCodes.Status404NotFound);
        return Success(review);
    }
}

