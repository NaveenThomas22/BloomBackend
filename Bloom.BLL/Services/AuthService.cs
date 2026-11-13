using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Constants;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bloom.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly AppDefaultsOptions _appDefaults;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger,
        IOptions<AppDefaultsOptions>? appDefaults = null)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _jwtOptions = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appDefaults = appDefaults?.Value ?? new AppDefaultsOptions();
    }

    public async Task<(AuthResponseDto Response, string RawRefreshToken)> RegisterAsync(
        RegisterRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();

        if (await _userRepository.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogWarning("Registration failed: Email {Email} is already registered.", request.Email);
            throw new ValidationException("Email", "An account with this email address already exists.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            IsActive = true,
            EmailVerified = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var targetYear = request.CatTargetYear 
            ?? _appDefaults.DefaultCatTargetYear 
            ?? (ushort)(DateTime.UtcNow.Month >= 12 ? DateTime.UtcNow.Year + 1 : DateTime.UtcNow.Year);

        var profile = new UserProfile
        {
            CatTargetYear = targetYear,
            PreparationLevel = !string.IsNullOrWhiteSpace(request.PreparationLevel) 
                ? request.PreparationLevel 
                : _appDefaults.DefaultPreparationLevel,
            PreparationMode = !string.IsNullOrWhiteSpace(request.PreparationMode) 
                ? request.PreparationMode 
                : _appDefaults.DefaultPreparationMode,
            Timezone = !string.IsNullOrWhiteSpace(request.Timezone) 
                ? request.Timezone 
                : _appDefaults.DefaultTimezone,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var preferences = new UserPreferences
        {
            Theme = !string.IsNullOrWhiteSpace(request.Theme) 
                ? request.Theme 
                : _appDefaults.DefaultTheme,
            DailyStudyTargetMinutes = request.DailyStudyTargetMinutes 
                ?? _appDefaults.DefaultDailyStudyTargetMinutes,
            PreferredWakeTime = new TimeSpan(6, 0, 0),
            PreferredSleepTime = new TimeSpan(22, 30, 0),
            NotificationEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var userId = await _userRepository.CreateAsync(user, profile, preferences, cancellationToken);
        user.Id = userId;
        user.Profile = profile;
        user.Preferences = preferences;

        _logger.LogInformation("New user registered successfully with ID {UserId}.", userId);

        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();
        var familyId = Guid.NewGuid().ToString("N");

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            TokenFamilyId = familyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user);

        var responseDto = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresInMinutes = _jwtOptions.AccessTokenLifetimeMinutes,
            User = MapToUserDto(user)
        };

        return (responseDto, rawToken);
    }

    public async Task<(AuthResponseDto Response, string RawRefreshToken)> LoginAsync(
        LoginRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        // Constant-time check: execute dummy hash verify if user not found to prevent user enumeration via timing
        if (user == null)
        {
            _passwordHasher.VerifyPassword("dummy_password", "$2a$12$e8h.7yQ7q5aE8q3I1v7nveRz2B1L.sY9d1vY0e3n.g1bY8Z3s9a1m");
            _logger.LogWarning("Failed login attempt for non-existent email {Email}.", request.Email);
            throw new UnauthorizedException("Invalid email address or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt rejected for inactive user ID {UserId}.", user.Id);
            throw new UnauthorizedException("Your account is currently disabled. Please contact support.");
        }

        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed password verification for user ID {UserId}.", user.Id);
            throw new UnauthorizedException("Invalid email address or password.");
        }

        await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, cancellationToken);

        // Fetch full profile and preferences
        var fullUser = await _userRepository.GetByIdAsync(user.Id, cancellationToken) ?? user;

        var (rawToken, tokenHash) = _tokenService.GenerateRefreshToken();
        var familyId = Guid.NewGuid().ToString("N");

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            TokenFamilyId = familyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(fullUser);

        _logger.LogInformation("User ID {UserId} logged in successfully.", user.Id);

        var responseDto = new AuthResponseDto
        {
            AccessToken = accessToken,
            ExpiresInMinutes = _jwtOptions.AccessTokenLifetimeMinutes,
            User = MapToUserDto(fullUser)
        };

        return (responseDto, rawToken);
    }

    public async Task<(AuthResponseDto Response, string RawRefreshToken)> RefreshTokenAsync(
        string rawRefreshToken, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedException("Refresh token was not provided.");
        }

        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Refresh attempt with completely unknown token hash.");
            throw new UnauthorizedException("Invalid refresh token.");
        }

        // Reuse detection check: token already revoked!
        if (storedToken.IsRevoked)
        {
            _logger.LogCritical(
                "Security alert: Revoked refresh token reuse detected for User ID {UserId}, Family {FamilyId}. Revoking family.",
                storedToken.UserId, storedToken.TokenFamilyId);

            await _refreshTokenRepository.RevokeFamilyAsync(
                storedToken.TokenFamilyId, 
                "Revoked due to token reuse attack attempt", 
                cancellationToken);

            throw new UnauthorizedException("Invalid or revoked refresh token. All active sessions have been terminated.");
        }

        if (storedToken.IsExpired)
        {
            _logger.LogWarning("Refresh token expired for User ID {UserId}.", storedToken.UserId);
            throw new UnauthorizedException("Refresh token has expired. Please log in again.");
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("User account is invalid or inactive.");
        }

        // Generate new token in the same family
        var (newRawToken, newTokenHash) = _tokenService.GenerateRefreshToken();
        var newTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            TokenFamilyId = storedToken.TokenFamilyId,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
            CreatedByIp = ipAddress,
            CreatedAtUtc = DateTime.UtcNow
        };

        var rotated = await _refreshTokenRepository.RotateTokenAsync(storedToken.Id, newTokenEntity, cancellationToken);
        if (!rotated)
        {
            _logger.LogWarning("Concurrent refresh attempt detected for token ID {TokenId}.", storedToken.Id);
            throw new UnauthorizedException("Could not rotate refresh token. Concurrent refresh attempt detected.");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);

        _logger.LogInformation("Refresh token successfully rotated for User ID {UserId}.", user.Id);

        var responseDto = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            ExpiresInMinutes = _jwtOptions.AccessTokenLifetimeMinutes,
            User = MapToUserDto(user)
        };

        return (responseDto, newRawToken);
    }

    public async Task<bool> LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return true; // Idempotent
        }

        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);
        var storedToken = await _refreshTokenRepository.GetByHashAsync(tokenHash, cancellationToken);

        if (storedToken == null || storedToken.IsRevoked)
        {
            return true;
        }

        await _refreshTokenRepository.RevokeAsync(storedToken.Id, reason: "User requested logout", cancellationToken: cancellationToken);
        _logger.LogInformation("Revoked refresh token ID {TokenId} for User ID {UserId}.", storedToken.Id, storedToken.UserId);
        return true;
    }

    public async Task<int> LogoutAllAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        var count = await _refreshTokenRepository.RevokeAllForUserAsync(userId, "Global logout requested", cancellationToken);
        _logger.LogInformation("Revoked all ({Count}) active sessions for User ID {UserId}.", count, userId);
        return count;
    }

    public async Task<UserDto> GetCurrentUserAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new NotFoundException("User", userId);
        }

        return MapToUserDto(user);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ProfilePictureUrl = user.ProfilePictureUrl,
            EmailVerified = user.EmailVerified,
            Role = user.Role ?? "Aspirant",
            CreatedAtUtc = user.CreatedAtUtc,
            Profile = user.Profile == null ? null : new UserProfileDto
            {
                DateOfBirth = user.Profile.DateOfBirth,
                CatTargetYear = user.Profile.CatTargetYear,
                CatExamDate = user.Profile.CatExamDate,
                PreparationLevel = user.Profile.PreparationLevel,
                PreparationMode = user.Profile.PreparationMode,
                Timezone = user.Profile.Timezone
            },
            Preferences = user.Preferences == null ? null : new UserPreferencesDto
            {
                Theme = user.Preferences.Theme,
                DailyStudyTargetMinutes = user.Preferences.DailyStudyTargetMinutes,
                PreferredWakeTime = user.Preferences.PreferredWakeTime.ToString(@"hh\:mm\:ss"),
                PreferredSleepTime = user.Preferences.PreferredSleepTime.ToString(@"hh\:mm\:ss"),
                NotificationEnabled = user.Preferences.NotificationEnabled
            }
        };
    }
}

