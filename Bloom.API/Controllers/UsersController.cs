using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(
        IUserService userService, 
        ICurrentUserService currentUserService)
    {
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>
    /// Gets the current authenticated aspirant's preparation profile.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var profile = await _userService.GetProfileAsync(userId, cancellationToken);
        return Success(profile, "Profile retrieved successfully.");
    }

    /// <summary>
    /// Updates the current authenticated aspirant's preparation profile.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateMyProfile(
        [FromBody] UpdateProfileRequestDto request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var profile = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        return Success(profile, "Profile updated successfully.");
    }

    /// <summary>
    /// Gets the current authenticated aspirant's UI and study preferences.
    /// </summary>
    [HttpGet("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> GetMyPreferences(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var preferences = await _userService.GetPreferencesAsync(userId, cancellationToken);
        return Success(preferences, "Preferences retrieved successfully.");
    }

    /// <summary>
    /// Updates the current authenticated aspirant's UI and study preferences.
    /// </summary>
    [HttpPut("me/preferences")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserPreferencesDto>>> UpdateMyPreferences(
        [FromBody] UpdatePreferencesRequestDto request, 
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var preferences = await _userService.UpdatePreferencesAsync(userId, request, cancellationToken);
        return Success(preferences, "Preferences updated successfully.");
    }
}

