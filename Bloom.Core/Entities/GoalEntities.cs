using Bloom.Core.Enums;

namespace Bloom.Core.Entities;

public class RevisionPolicy
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; } // Null for system defaults
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IntervalsCsv { get; set; } = "1,3,7,14,30";
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public IReadOnlyList<int> GetIntervals()
    {
        return IntervalsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 1)
            .ToList();
    }
}

public class RevisionSchedule
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong TopicId { get; set; }
    public ulong RevisionPolicyId { get; set; }
    public int StepNumber { get; set; } = 1;
    public DateOnly ScheduledDate { get; set; }
    public string Status { get; set; } = "Upcoming";
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class RevisionAttempt
{
    public ulong Id { get; set; }
    public ulong? RevisionScheduleId { get; set; }
    public ulong UserId { get; set; }
    public ulong TopicId { get; set; }
    public DateTime RevisedAtUtc { get; set; }
    public DateOnly RevisionDate { get; set; }
    public string UnderstandingLevel { get; set; } = "Good";
    public int ConfidenceRating { get; set; } = 3;
    public string? Notes { get; set; }
    public ulong? LinkedActivityRecordId { get; set; }
    public DateOnly? NextScheduledDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class UserGoal
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string GoalPeriod { get; set; } = "Daily";
    public string TargetMetric { get; set; } = "StudyMinutes";
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = "Minutes";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class PreparationMilestone
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly TargetDate { get; set; }
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

