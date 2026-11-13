using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _profileRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IUserProfileRepository profileRepository,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UserProfileDto> GetProfileAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            throw new NotFoundException("UserProfile", userId);
        }

        return MapToProfileDto(profile);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        ulong userId, 
        UpdateProfileRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new NotFoundException("User", userId);
        }

        // Update full name and profile picture URL on user record
        user.FullName = request.FullName.Trim();
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await _userRepository.UpdateAsync(user, cancellationToken);

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            profile = new UserProfile { UserId = userId };
        }

        profile.DateOfBirth = request.DateOfBirth;
        profile.CatTargetYear = request.CatTargetYear;
        profile.CatExamDate = request.CatExamDate;
        profile.PreparationLevel = request.PreparationLevel;
        profile.PreparationMode = request.PreparationMode;
        profile.Timezone = request.Timezone;

        await _profileRepository.UpdateProfileAsync(profile, cancellationToken);

        _logger.LogInformation("Profile updated successfully for User ID {UserId}.", userId);

        return MapToProfileDto(profile);
    }

    public async Task<UserPreferencesDto> GetPreferencesAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        var preferences = await _profileRepository.GetPreferencesByUserIdAsync(userId, cancellationToken);
        if (preferences == null)
        {
            throw new NotFoundException("UserPreferences", userId);
        }

        return MapToPreferencesDto(preferences);
    }

    public async Task<UserPreferencesDto> UpdatePreferencesAsync(
        ulong userId, 
        UpdatePreferencesRequestDto request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var preferences = await _profileRepository.GetPreferencesByUserIdAsync(userId, cancellationToken);
        if (preferences == null)
        {
            preferences = new UserPreferences { UserId = userId };
        }

        preferences.Theme = request.Theme;
        preferences.DailyStudyTargetMinutes = request.DailyStudyTargetMinutes;
        preferences.PreferredWakeTime = TimeSpan.Parse(request.PreferredWakeTime);
        preferences.PreferredSleepTime = TimeSpan.Parse(request.PreferredSleepTime);
        preferences.NotificationEnabled = request.NotificationEnabled;

        await _profileRepository.UpdatePreferencesAsync(preferences, cancellationToken);

        _logger.LogInformation("Preferences updated successfully for User ID {UserId}.", userId);

        return MapToPreferencesDto(preferences);
    }

    private static UserProfileDto MapToProfileDto(UserProfile profile)
    {
        return new UserProfileDto
        {
            DateOfBirth = profile.DateOfBirth,
            CatTargetYear = profile.CatTargetYear,
            CatExamDate = profile.CatExamDate,
            PreparationLevel = profile.PreparationLevel,
            PreparationMode = profile.PreparationMode,
            Timezone = profile.Timezone
        };
    }

    private static UserPreferencesDto MapToPreferencesDto(UserPreferences preferences)
    {
        return new UserPreferencesDto
        {
            Theme = preferences.Theme,
            DailyStudyTargetMinutes = preferences.DailyStudyTargetMinutes,
            PreferredWakeTime = preferences.PreferredWakeTime.ToString(@"hh\:mm\:ss"),
            PreferredSleepTime = preferences.PreferredSleepTime.ToString(@"hh\:mm\:ss"),
            NotificationEnabled = preferences.NotificationEnabled
        };
    }
}

