using Bloom.Core.Interfaces;

namespace Bloom.Core.Entities;

public class ActivityCategory : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; } // Null indicates shared system default
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "activity";
    public string Color { get; set; } = "#A8BDA5";
    public string CategoryGroup { get; set; } = "Other";
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ActivityRecord : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong ActivityCategoryId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public string ActivityStatus { get; set; } = "Completed";
    public string EntrySource { get; set; } = "Manual";
    public bool IsPlanned { get; set; }
    public string? SessionGroupId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAtUtc { get; set; }

    // Helper property (not mapped directly as a column to prevent duplicate duration storage)
    public double DurationMinutes => EndAtUtc.HasValue && EndAtUtc.Value > StartAtUtc
        ? (EndAtUtc.Value - StartAtUtc).TotalMinutes
        : 0;
}

public class StudySessionDetail
{
    public ulong ActivityId { get; set; }
    public ulong? TopicId { get; set; }
    public string StudyMode { get; set; } = "Practice";
    public string DifficultyLevel { get; set; } = "Moderate";
    public uint QuestionsAttempted { get; set; }
    public uint QuestionsCorrect { get; set; }
    public string? SessionNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class PlannedActivity : IAuditableEntity
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong ActivityCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PlannedStartAtUtc { get; set; }
    public DateTime PlannedEndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public ulong? LinkedTopicId { get; set; }
    public string PlanStatus { get; set; } = "Scheduled";
    public ulong? ActualActivityId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public double PlannedDurationMinutes => PlannedEndAtUtc > PlannedStartAtUtc
        ? (PlannedEndAtUtc - PlannedStartAtUtc).TotalMinutes
        : 0;
}

public class UserTimerState
{
    public ulong UserId { get; set; }
    public string SessionGroupId { get; set; } = string.Empty;
    public ulong ActivityCategoryId { get; set; }
    public ulong? TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StudyMode { get; set; } = "Practice";
    public string TimerState { get; set; } = "Running";
    public ulong? CurrentIntervalId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? LastPausedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

