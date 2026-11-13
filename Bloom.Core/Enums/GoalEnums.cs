namespace Bloom.Core.Enums;

public enum GoalPeriod
{
    Daily,
    Weekly,
    Monthly,
    Custom
}

public enum TargetMetric
{
    StudyMinutes,
    TopicsCompleted,
    SubtopicsCompleted,
    RevisionsCompleted,
    ActivitiesCount
}

public enum GoalUnit
{
    Minutes,
    Hours,
    Topics,
    Subtopics,
    Revisions,
    Sessions
}

public enum GoalStatus
{
    Active,
    Paused,
    Completed,
    Cancelled
}

public enum MilestonePriority
{
    Low,
    Medium,
    High,
    Urgent
}

public enum MilestoneStatus
{
    Pending,
    Completed,
    Overdue,
    Cancelled
}

public enum RevisionScheduleStatus
{
    Upcoming,
    DueToday,
    Overdue,
    Completed,
    Skipped,
    Rescheduled
}

public enum UnderstandingLevel
{
    Difficult,
    Partial,
    Good,
    Mastered
}

