namespace Bloom.Core.Entities;

/// <summary>
/// Domain model for an aspirant's study and UI preferences.
/// </summary>
public class UserPreferences : BaseEntity
{
    public ulong UserId { get; set; }
    public string Theme { get; set; } = "Sage Garden";
    public uint DailyStudyTargetMinutes { get; set; } = 360; // 6 hours
    public TimeSpan PreferredWakeTime { get; set; } = new(6, 0, 0); // 06:00:00
    public TimeSpan PreferredSleepTime { get; set; } = new(22, 30, 0); // 22:30:00
    public bool NotificationEnabled { get; set; } = true;
}

