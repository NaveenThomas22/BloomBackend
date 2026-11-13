using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/passages")]
public class QuestionPassagesController : BaseApiController
{
    private readonly IQuestionPassageService _passageService;

    public QuestionPassagesController(IQuestionPassageService passageService)
    {
        _passageService = passageService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuestionPassageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QuestionPassageDto>>>> GetPassages(
        [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var list = await _passageService.GetPassagesAsync(activeOnly, ct);
        return Success(list);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionPassageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QuestionPassageDto>>> GetPassage([FromRoute] ulong id, CancellationToken ct = default)
    {
        var p = await _passageService.GetPassageByIdAsync(id, ct);
        if (p == null) return Failure<QuestionPassageDto>($"Passage {id} not found.", StatusCodes.Status404NotFound);
        return Success(p);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionPassageDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<QuestionPassageDto>>> CreatePassage(
        [FromBody] CreatePassageRequest request, CancellationToken ct = default)
    {
        var created = await _passageService.CreatePassageAsync(request, ct);
        return CreatedSuccess(created);
    }
}

[Authorize]
[Route("api/v1/question-sets")]
public class QuestionSetsController : BaseApiController
{
    private readonly IQuestionSetService _setService;

    public QuestionSetsController(IQuestionSetService setService)
    {
        _setService = setService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuestionSetDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<QuestionSetDto>>>> GetSets(
        [FromQuery] bool activeOnly = true, CancellationToken ct = default)
    {
        var list = await _setService.GetSetsAsync(activeOnly, ct);
        return Success(list);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<QuestionSetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<QuestionSetDto>>> GetSet([FromRoute] ulong id, CancellationToken ct = default)
    {
        var s = await _setService.GetSetByIdAsync(id, ct);
        if (s == null) return Failure<QuestionSetDto>($"Question set {id} not found.", StatusCodes.Status404NotFound);
        return Success(s);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentEditor")]
    [ProducesResponseType(typeof(ApiResponse<QuestionSetDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<QuestionSetDto>>> CreateSet(
        [FromBody] CreateQuestionSetRequest request, CancellationToken ct = default)
    {
        var created = await _setService.CreateSetAsync(request, ct);
        return CreatedSuccess(created);
    }
}

