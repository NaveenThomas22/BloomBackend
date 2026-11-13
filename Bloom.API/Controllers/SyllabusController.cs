using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class SyllabusController : BaseApiController
{
    private readonly ISyllabusService _syllabusService;
    private readonly ICurrentUserService _currentUserService;

    public SyllabusController(
        ISyllabusService syllabusService,
        ICurrentUserService currentUserService)
    {
        _syllabusService = syllabusService ?? throw new ArgumentNullException(nameof(syllabusService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    #region Hierarchy Navigation & Progress Summaries

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<SyllabusOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SyllabusOverviewDto>>> GetOverview(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var overview = await _syllabusService.GetOverviewAsync(userId, ct);
        return Success(overview, "Syllabus overview and progress retrieved successfully.");
    }

    [HttpGet("subjects")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SubjectSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubjectSummaryDto>>>> GetSubjects(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var subjects = await _syllabusService.GetSubjectsAsync(userId, ct);
        return Success(subjects, "Syllabus subjects retrieved successfully.");
    }

    [HttpGet("subjects/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SubjectDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubjectDetailDto>>> GetSubjectDetail([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var subject = await _syllabusService.GetSubjectDetailAsync(userId, id, ct);
        return Success(subject, "Subject detail retrieved successfully.");
    }

    [HttpGet("modules/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ModuleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModuleDetailDto>>> GetModuleDetail([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var module = await _syllabusService.GetModuleDetailAsync(userId, id, ct);
        return Success(module, "Module detail retrieved successfully.");
    }

    [HttpGet("topics/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TopicDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TopicDetailDto>>> GetTopicDetail([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var topic = await _syllabusService.GetTopicDetailAsync(userId, id, ct);
        return Success(topic, "Topic detail retrieved successfully.");
    }

    #endregion

    #region Customization - Subjects

    [HttpPost("subjects")]
    [ProducesResponseType(typeof(ApiResponse<SubjectSummaryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> CreateSubject([FromBody] CreateSubjectDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _syllabusService.CreateCustomSubjectAsync(userId, dto, ct);
        return CreatedSuccess(created, "Custom subject created successfully.");
    }

    [HttpPut("subjects/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SubjectSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubjectSummaryDto>>> UpdateSubject([FromRoute] ulong id, [FromBody] UpdateSubjectDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateSubjectAsync(userId, id, dto, ct);
        return Success(updated, "Subject updated successfully.");
    }

    [HttpDelete("subjects/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteSubject([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _syllabusService.DeleteSubjectAsync(userId, id, ct);
        return Success("Custom subject deleted successfully.");
    }

    #endregion

    #region Customization - Modules

    [HttpPost("modules")]
    [ProducesResponseType(typeof(ApiResponse<ModuleDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ModuleDetailDto>>> CreateModule([FromBody] CreateModuleDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _syllabusService.CreateCustomModuleAsync(userId, dto, ct);
        return CreatedSuccess(created, "Custom module created successfully.");
    }

    [HttpPut("modules/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ModuleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModuleDetailDto>>> UpdateModule([FromRoute] ulong id, [FromBody] UpdateModuleDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateModuleAsync(userId, id, dto, ct);
        return Success(updated, "Module updated successfully.");
    }

    [HttpDelete("modules/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteModule([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _syllabusService.DeleteModuleAsync(userId, id, ct);
        return Success("Custom module deleted successfully.");
    }

    #endregion

    #region Customization - Topics

    [HttpPost("topics")]
    [ProducesResponseType(typeof(ApiResponse<TopicDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<TopicDetailDto>>> CreateTopic([FromBody] CreateTopicDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _syllabusService.CreateCustomTopicAsync(userId, dto, ct);
        return CreatedSuccess(created, "Custom topic created successfully.");
    }

    [HttpPut("topics/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TopicDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TopicDetailDto>>> UpdateTopic([FromRoute] ulong id, [FromBody] UpdateTopicDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateTopicAsync(userId, id, dto, ct);
        return Success(updated, "Topic updated successfully.");
    }

    [HttpDelete("topics/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteTopic([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _syllabusService.DeleteTopicAsync(userId, id, ct);
        return Success("Custom topic deleted successfully.");
    }

    #endregion

    #region Customization - Subtopics

    [HttpPost("subtopics")]
    [ProducesResponseType(typeof(ApiResponse<SubtopicDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<SubtopicDetailDto>>> CreateSubtopic([FromBody] CreateSubtopicDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _syllabusService.CreateCustomSubtopicAsync(userId, dto, ct);
        return CreatedSuccess(created, "Custom subtopic created successfully.");
    }

    [HttpPut("subtopics/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<SubtopicDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubtopicDetailDto>>> UpdateSubtopic([FromRoute] ulong id, [FromBody] UpdateSubtopicDto dto, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateSubtopicAsync(userId, id, dto, ct);
        return Success(updated, "Subtopic updated successfully.");
    }

    [HttpDelete("subtopics/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteSubtopic([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _syllabusService.DeleteSubtopicAsync(userId, id, ct);
        return Success("Custom subtopic deleted successfully.");
    }

    #endregion

    #region Learning Progress Updates

    [HttpPut("topics/{id:long}/progress")]
    [ProducesResponseType(typeof(ApiResponse<TopicSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TopicSummaryDto>>> UpdateTopicProgress(
        [FromRoute] ulong id,
        [FromBody] UpdateTopicProgressDto dto,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateTopicProgressAsync(userId, id, dto, ct);
        return Success(updated, "Topic learning progress updated successfully.");
    }

    [HttpPut("subtopics/{id:long}/progress")]
    [ProducesResponseType(typeof(ApiResponse<SubtopicDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SubtopicDetailDto>>> UpdateSubtopicProgress(
        [FromRoute] ulong id,
        [FromBody] UpdateSubtopicProgressDto dto,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _syllabusService.UpdateSubtopicProgressAsync(userId, id, dto, ct);
        return Success(updated, "Subtopic learning progress updated successfully.");
    }

    #endregion

    #region Study Resources

    [HttpPost("topics/{id:long}/resources")]
    [ProducesResponseType(typeof(ApiResponse<SyllabusResourceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SyllabusResourceDto>>> AddTopicResource(
        [FromRoute] ulong id,
        [FromBody] CreateResourceDto dto,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _syllabusService.AddTopicResourceAsync(userId, id, dto, ct);
        return CreatedSuccess(created, "Resource attached to topic successfully.");
    }

    [HttpDelete("resources/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteResource([FromRoute] ulong id, CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _syllabusService.DeleteTopicResourceAsync(userId, id, ct);
        return Success("Study resource deleted successfully.");
    }

    #endregion
}

