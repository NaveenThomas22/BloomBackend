using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/personal-milestones")]
public class PersonalMilestonesController : BaseApiController
{
    private readonly IPersonalMilestoneService _milestoneService;
    private readonly ICurrentUserService _currentUserService;

    public PersonalMilestonesController(
        IPersonalMilestoneService milestoneService,
        ICurrentUserService currentUserService)
    {
        _milestoneService = milestoneService ?? throw new ArgumentNullException(nameof(milestoneService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalMilestoneDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PersonalMilestoneDto>>>> GetMilestones(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestones = await _milestoneService.GetMilestonesAsync(userId, status, ct);
        return Success(milestones, "Personal milestones retrieved successfully.");
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<MilestoneOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MilestoneOverviewDto>>> GetOverview(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var overview = await _milestoneService.GetOverviewAsync(userId, ct);
        return Success(overview, "Milestone overview retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PersonalMilestoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PersonalMilestoneDto>>> GetMilestoneById(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestone = await _milestoneService.GetMilestoneByIdAsync(id, userId, ct);
        if (milestone == null)
        {
            return Failure<PersonalMilestoneDto>($"Milestone with ID {id} not found.", StatusCodes.Status404NotFound);
        }

        return Success(milestone, "Milestone retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PersonalMilestoneDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<PersonalMilestoneDto>>> CreateMilestone(
        [FromBody] CreateMilestoneRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestone = await _milestoneService.CreateMilestoneAsync(userId, request, ct);
        return CreatedSuccess(milestone, "Milestone created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PersonalMilestoneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PersonalMilestoneDto>>> UpdateMilestone(
        [FromRoute] ulong id,
        [FromBody] UpdateMilestoneRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestone = await _milestoneService.UpdateMilestoneAsync(id, userId, request, ct);
        return Success(milestone!, "Milestone updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteMilestone(
        [FromRoute] ulong id,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var success = await _milestoneService.DeleteMilestoneAsync(id, userId, ct);
        return Success(success, "Milestone deleted successfully.");
    }

    [HttpPost("sync")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> SyncProgress(CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _milestoneService.SyncMilestoneProgressAsync(userId, ct);
        return Success(updated, $"Synced progress for {updated} milestones.");
    }
}
