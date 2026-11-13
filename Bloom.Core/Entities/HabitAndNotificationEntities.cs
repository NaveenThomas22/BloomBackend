using Bloom.Core.Interfaces;

namespace Bloom.Core.Entities;

public class HabitCategory : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; } // Null for system defaults
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "check-circle";
    public string Color { get; set; } = "#A8BDA5";
    public ushort DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserHabit : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Frequency { get; set; } = "Daily"; // Daily, Weekdays, Weekends, CustomDays
    public uint TargetFrequencyDays { get; set; } = 7;
    public decimal TargetValue { get; set; } = 1.00m;
    public string TargetUnit { get; set; } = "times";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    // Joined Category details
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryIcon { get; set; }
    public string? CategoryColor { get; set; }
}

public class HabitCompletion : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong HabitId { get; set; }
    public DateOnly CompletionDate { get; set; }
    public string Status { get; set; } = "Completed"; // Completed, Partial, Skipped
    public decimal ActualValue { get; set; } = 1.00m;
    public string? SkipReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class HabitStreakInfo
{
    public ulong HabitId { get; set; }
    public string HabitName { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalCompletions { get; set; }
    public double CompletionRate30Days { get; set; }
    public DateOnly? LastCompletedDate { get; set; }
}

public class PersonalMilestone : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "GENERAL"; // STUDY_HOURS, QUESTIONS_SOLVED, TOPICS_MASTERED, MOCK_TESTS, STREAK, GENERAL
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = "count";
    public DateOnly? TargetDate { get; set; }
    public string Status { get; set; } = "InProgress"; // InProgress, Achieved, Abandoned
    public DateTime? AchievedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }
}

public class NotificationCategory
{
    public ulong Id { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "bell";
    public string Color { get; set; } = "#A8BDA5";
    public bool DefaultEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserNotificationPreference : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public TimeSpan? PreferredTimeUtc { get; set; }
    public TimeSpan? QuietHoursStartUtc { get; set; }
    public TimeSpan? QuietHoursEndUtc { get; set; }
    public string Channels { get; set; } = "IN_APP,PUSH";
    public uint MaxDailyAlerts { get; set; } = 10;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UserNotification : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string Status { get; set; } = "Unread"; // Unread, Read, Dismissed
    public DateTime? ReadAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class NotificationSubscription
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class NotificationDeliveryAttempt
{
    public ulong Id { get; set; }
    public ulong NotificationId { get; set; }
    public string Channel { get; set; } = "PUSH";
    public string Status { get; set; } = "Success"; // Success, Failed, SkippedQuietHours
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
}

public class MilestoneSystemMetrics
{
    public decimal StudyHours { get; set; }
    public int QuestionsSolved { get; set; }
    public int TopicsMastered { get; set; }
    public int MockTestsCompleted { get; set; }
}

public class SmartNotificationTriggersData
{
    public int DueRevisionsCount { get; set; }
    public bool MorningReflectionLogged { get; set; }
    public bool EveningReflectionLogged { get; set; }
    public int PendingHabitsCount { get; set; }
}
