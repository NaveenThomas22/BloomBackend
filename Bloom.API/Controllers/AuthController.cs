using Bloom.API.Filters;
using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Constants;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Bloom.API.Controllers;

[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private const string RefreshTokenCookieName = "bloom_refresh_token";

    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly JwtOptions _jwtOptions;
    private readonly IHostEnvironment _environment;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        IOptions<JwtOptions> jwtOptions,
        IHostEnvironment environment)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    [HttpPost("register")]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterRequestDto request, 
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (response, rawRefreshToken) = await _authService.RegisterAsync(request, ipAddress, cancellationToken);

        SetRefreshTokenCookie(rawRefreshToken);
        return CreatedSuccess(response, "Aspirant account registered successfully.");
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginRequestDto request, 
        CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (response, rawRefreshToken) = await _authService.LoginAsync(request, ipAddress, cancellationToken);

        SetRefreshTokenCookie(rawRefreshToken);
        return Success(response, "Authenticated successfully.");
    }

    [HttpPost("refresh")]
    [ValidateCsrf]
    [EnableRateLimiting("AuthRateLimit")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedException("No refresh token cookie found in request.");
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var (response, newRawRefreshToken) = await _authService.RefreshTokenAsync(rawRefreshToken, ipAddress, cancellationToken);

        SetRefreshTokenCookie(newRawRefreshToken);
        return Success(response, "Token session refreshed successfully.");
    }

    [HttpPost("logout")]
    [ValidateCsrf]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> Logout(CancellationToken cancellationToken)
    {
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

        if (!string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            await _authService.LogoutAsync(rawRefreshToken, cancellationToken);
        }

        DeleteRefreshTokenCookie();
        return Success("Logged out successfully.");
    }

    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> LogoutAll(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        await _authService.LogoutAllAsync(userId, cancellationToken);

        DeleteRefreshTokenCookie();
        return Success("All active sessions terminated successfully.");
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Success(user, "User profile retrieved successfully.");
    }

    private void SetRefreshTokenCookie(string rawRefreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !_environment.IsDevelopment(),
            SameSite = ResolveSameSiteMode(),
            Path = "/api/v1/auth",
            Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays)
        };

        Response.Cookies.Append(RefreshTokenCookieName, rawRefreshToken, cookieOptions);
    }

    private void DeleteRefreshTokenCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps || !_environment.IsDevelopment(),
            SameSite = ResolveSameSiteMode(),
            Path = "/api/v1/auth"
        };

        Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
    }

    private SameSiteMode ResolveSameSiteMode()
    {
        return _jwtOptions.RefreshTokenSameSite?.ToLowerInvariant() switch
        {
            "lax" => SameSiteMode.Lax,
            "none" => SameSiteMode.None,
            _ => SameSiteMode.Strict
        };
    }
}

