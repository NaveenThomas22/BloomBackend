namespace Bloom.BLL.DTOs;

public class RegisterRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public ushort? CatTargetYear { get; set; }
    public string? PreparationLevel { get; set; }
    public string? PreparationMode { get; set; }
    public string? Timezone { get; set; }
    public string? Theme { get; set; }
    public uint? DailyStudyTargetMinutes { get; set; }
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public ulong Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool EmailVerified { get; set; }
    public string Role { get; set; } = "Aspirant";
    public DateTime CreatedAtUtc { get; set; }
    public UserProfileDto? Profile { get; set; }
    public UserPreferencesDto? Preferences { get; set; }
}

public class UserProfileDto
{
    public DateTime? DateOfBirth { get; set; }
    public ushort CatTargetYear { get; set; }
    public DateTime? CatExamDate { get; set; }
    public string PreparationLevel { get; set; } = string.Empty;
    public string PreparationMode { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}

public class UserPreferencesDto
{
    public string Theme { get; set; } = string.Empty;
    public uint DailyStudyTargetMinutes { get; set; }
    public string PreferredWakeTime { get; set; } = string.Empty;
    public string PreferredSleepTime { get; set; } = string.Empty;
    public bool NotificationEnabled { get; set; }
}

public class UpdateProfileRequestDto
{
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public ushort CatTargetYear { get; set; }
    public DateTime? CatExamDate { get; set; }
    public string PreparationLevel { get; set; } = "Beginner";
    public string PreparationMode { get; set; } = "SelfStudy";
    public string Timezone { get; set; } = "Asia/Kolkata";
}

public class UpdatePreferencesRequestDto
{
    public string Theme { get; set; } = "Sage Garden";
    public uint DailyStudyTargetMinutes { get; set; } = 360;
    public string PreferredWakeTime { get; set; } = "06:00:00";
    public string PreferredSleepTime { get; set; } = "22:30:00";
    public bool NotificationEnabled { get; set; } = true;
}

