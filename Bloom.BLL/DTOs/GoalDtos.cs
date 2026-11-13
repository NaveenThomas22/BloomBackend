using Bloom.Core.Enums;

namespace Bloom.BLL.DTOs;

#region Goal DTOs

public class UserGoalDto
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
    public string? SubjectName { get; set; }
    public ulong? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string Status { get; set; } = "Active";
    public decimal CurrentValue { get; set; }
    public decimal ProgressPercentage { get; set; }
    public bool IsAchieved { get; set; }
    public int DaysRemaining { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateGoalDto
{
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
}

public class UpdateGoalDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = "Minutes";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public string Status { get; set; } = "Active";
}

public class GoalDashboardSummaryDto
{
    public string Period { get; set; } = "Daily";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalGoals { get; set; }
    public int AchievedGoals { get; set; }
    public decimal AverageProgressPercentage { get; set; }
    public IReadOnlyList<UserGoalDto> Goals { get; set; } = Array.Empty<UserGoalDto>();
}

#endregion

#region Milestone DTOs

public class PreparationMilestoneDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly TargetDate { get; set; }
    public ulong? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public ulong? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly TargetDate { get; set; }
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public string Priority { get; set; } = "Medium";
}

public class UpdateMilestoneDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly TargetDate { get; set; }
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Pending";
}

#endregion

#region Revision Policy DTOs

public class RevisionPolicyDto
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IntervalsCsv { get; set; } = string.Empty;
    public IReadOnlyList<int> Intervals { get; set; } = Array.Empty<int>();
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateRevisionPolicyDto
{
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IntervalsCsv { get; set; } = "1,3,7,14,30";
}

public class UpdateRevisionPolicyDto
{
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IntervalsCsv { get; set; } = "1,3,7,14,30";
    public bool IsActive { get; set; } = true;
}

#endregion

#region Revision Schedule & Attempt DTOs

public class RevisionScheduleDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public ulong? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public ulong RevisionPolicyId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public string Status { get; set; } = "Upcoming";
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class RevisionAttemptDto
{
    public ulong Id { get; set; }
    public ulong? RevisionScheduleId { get; set; }
    public ulong UserId { get; set; }
    public ulong TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public DateTime RevisedAtUtc { get; set; }
    public DateOnly RevisionDate { get; set; }
    public string UnderstandingLevel { get; set; } = "Good";
    public int ConfidenceRating { get; set; }
    public string? Notes { get; set; }
    public ulong? LinkedActivityRecordId { get; set; }
    public DateOnly? NextScheduledDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class RecordRevisionAttemptRequest
{
    public ulong? RevisionScheduleId { get; set; }
    public ulong TopicId { get; set; }
    public string UnderstandingLevel { get; set; } = "Good";
    public int ConfidenceRating { get; set; } = 3;
    public string? Notes { get; set; }
    public int? MinutesSpent { get; set; }
    public bool CreateActivityRecord { get; set; } = true;
}

public class RescheduleRevisionRequest
{
    public DateOnly NewDate { get; set; }
    public string? Reason { get; set; }
}

public class RevisionDashboardSummaryDto
{
    public int DueTodayCount { get; set; }
    public int OverdueCount { get; set; }
    public int UpcomingCount { get; set; }
    public int CompletedTotalCount { get; set; }
    public IReadOnlyList<RevisionScheduleDto> DueToday { get; set; } = Array.Empty<RevisionScheduleDto>();
    public IReadOnlyList<RevisionScheduleDto> Overdue { get; set; } = Array.Empty<RevisionScheduleDto>();
    public IReadOnlyList<RevisionScheduleDto> Upcoming { get; set; } = Array.Empty<RevisionScheduleDto>();
}

#endregion

