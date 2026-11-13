using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/milestones")]
public class MilestonesController : BaseApiController
{
    private readonly IPreparationMilestoneService _milestoneService;
    private readonly ICurrentUserService _currentUserService;

    public MilestonesController(
        IPreparationMilestoneService milestoneService,
        ICurrentUserService currentUserService)
    {
        _milestoneService = milestoneService ?? throw new ArgumentNullException(nameof(milestoneService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PreparationMilestoneDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PreparationMilestoneDto>>>> GetMilestones(
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestones = await _milestoneService.GetMilestonesAsync(userId, status, ct);
        return Success(milestones, "Milestones retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PreparationMilestoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PreparationMilestoneDto>>> GetMilestoneById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var milestone = await _milestoneService.GetMilestoneByIdAsync(id, userId, ct);
        return Success(milestone, "Milestone retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PreparationMilestoneDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PreparationMilestoneDto>>> CreateMilestone(
        [FromBody] CreateMilestoneDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _milestoneService.CreateMilestoneAsync(userId, dto, ct);
        return CreatedSuccess(created, "Milestone created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<PreparationMilestoneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PreparationMilestoneDto>>> UpdateMilestone(
        [FromRoute] ulong id,
        [FromBody] UpdateMilestoneDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _milestoneService.UpdateMilestoneAsync(id, userId, dto, ct);
        return Success(updated, "Milestone updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeleteMilestone(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _milestoneService.DeleteMilestoneAsync(id, userId, ct);
        return Success("Milestone deleted successfully.");
    }
}

