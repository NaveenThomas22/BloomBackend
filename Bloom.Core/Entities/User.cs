namespace Bloom.Core.Entities;

/// <summary>
/// Domain model for an aspirant account.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool EmailVerified { get; set; } = false;
    public string Role { get; set; } = "Aspirant";
    public DateTime? LastLoginAtUtc { get; set; }

    // Navigation properties for convenience
    public UserProfile? Profile { get; set; }
    public UserPreferences? Preferences { get; set; }
}

