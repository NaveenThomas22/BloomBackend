using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/revision-policies")]
public class RevisionPoliciesController : BaseApiController
{
    private readonly IRevisionPolicyService _policyService;
    private readonly ICurrentUserService _currentUserService;

    public RevisionPoliciesController(
        IRevisionPolicyService policyService,
        ICurrentUserService currentUserService)
    {
        _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RevisionPolicyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevisionPolicyDto>>>> GetPolicies(
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var policies = await _policyService.GetPoliciesAsync(userId, ct);
        return Success(policies, "Revision policies retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<RevisionPolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RevisionPolicyDto>>> GetPolicyById(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var policy = await _policyService.GetPolicyByIdAsync(id, userId, ct);
        return Success(policy, "Revision policy retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RevisionPolicyDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<RevisionPolicyDto>>> CreatePolicy(
        [FromBody] CreateRevisionPolicyDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var created = await _policyService.CreatePolicyAsync(userId, dto, ct);
        return CreatedSuccess(created, "Revision policy created successfully.");
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<RevisionPolicyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RevisionPolicyDto>>> UpdatePolicy(
        [FromRoute] ulong id,
        [FromBody] UpdateRevisionPolicyDto dto,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var updated = await _policyService.UpdatePolicyAsync(id, userId, dto, ct);
        return Success(updated, "Revision policy updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeletePolicy(
        [FromRoute] ulong id,
        CancellationToken ct = default)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _policyService.DeletePolicyAsync(id, userId, ct);
        return Success("Revision policy deleted successfully.");
    }
}

