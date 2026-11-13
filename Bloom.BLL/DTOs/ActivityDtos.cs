namespace Bloom.BLL.DTOs;

#region Activity Category DTOs

public class ActivityCategoryDto
{
    public ulong Id { get; set; }
    public ulong? UserId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "activity";
    public string Color { get; set; } = "#A8BDA5";
    public string CategoryGroup { get; set; } = "Other";
    public bool IsActive { get; set; }
    public ushort DisplayOrder { get; set; }
    public bool IsCustom { get; set; }
}

public class CreateCategoryDto
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "activity";
    public string Color { get; set; } = "#A8BDA5";
    public string CategoryGroup { get; set; } = "Other";
    public ushort DisplayOrder { get; set; } = 1;
}

public class UpdateCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "activity";
    public string Color { get; set; } = "#A8BDA5";
    public string CategoryGroup { get; set; } = "Other";
    public bool IsActive { get; set; } = true;
    public ushort DisplayOrder { get; set; } = 1;
}

#endregion

#region Activity Record & Study Session DTOs

public class StudySessionDetailDto
{
    public ulong ActivityId { get; set; }
    public ulong? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? ModuleName { get; set; }
    public string? SubjectCode { get; set; }
    public string? SubjectName { get; set; }
    public string StudyMode { get; set; } = "Practice";
    public string DifficultyLevel { get; set; } = "Moderate";
    public uint QuestionsAttempted { get; set; }
    public uint QuestionsCorrect { get; set; }
    public string? SessionNotes { get; set; }
}

public class ActivityRecordDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong ActivityCategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryGroup { get; set; } = "Other";
    public string Color { get; set; } = "#A8BDA5";
    public string Icon { get; set; } = "activity";
    public string ActivityTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public string ActivityStatus { get; set; } = "Completed";
    public string EntrySource { get; set; } = "Manual";
    public bool IsPlanned { get; set; }
    public string? SessionGroupId { get; set; }
    public double DurationMinutes { get; set; }
    public StudySessionDetailDto? StudyDetails { get; set; }
}

public class CreateActivityRecordDto
{
    public ulong ActivityCategoryId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public string EntrySource { get; set; } = "Manual";
    public bool IsPlanned { get; set; }
    public string? SessionGroupId { get; set; }

    // Optional study extension
    public ulong? TopicId { get; set; }
    public string? StudyMode { get; set; }
    public string? DifficultyLevel { get; set; }
    public uint QuestionsAttempted { get; set; }
    public uint QuestionsCorrect { get; set; }
    public string? SessionNotes { get; set; }
}

public class UpdateActivityRecordDto
{
    public ulong ActivityCategoryId { get; set; }
    public string ActivityTitle { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";

    // Optional study extension
    public ulong? TopicId { get; set; }
    public string? StudyMode { get; set; }
    public string? DifficultyLevel { get; set; }
    public uint QuestionsAttempted { get; set; }
    public uint QuestionsCorrect { get; set; }
    public string? SessionNotes { get; set; }
}

#endregion

#region Planned Activities DTOs

public class PlannedActivityDto
{
    public ulong Id { get; set; }
    public ulong UserId { get; set; }
    public ulong ActivityCategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryGroup { get; set; } = "Other";
    public string Color { get; set; } = "#A8BDA5";
    public string Icon { get; set; } = "activity";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PlannedStartAtUtc { get; set; }
    public DateTime PlannedEndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public ulong? LinkedTopicId { get; set; }
    public string? LinkedTopicName { get; set; }
    public string PlanStatus { get; set; } = "Scheduled";
    public ulong? ActualActivityId { get; set; }
    public double PlannedDurationMinutes { get; set; }
}

public class CreatePlannedActivityDto
{
    public ulong ActivityCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PlannedStartAtUtc { get; set; }
    public DateTime PlannedEndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public ulong? LinkedTopicId { get; set; }
}

public class UpdatePlannedActivityDto
{
    public ulong ActivityCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime PlannedStartAtUtc { get; set; }
    public DateTime PlannedEndAtUtc { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public ulong? LinkedTopicId { get; set; }
    public string PlanStatus { get; set; } = "Scheduled";
}

#endregion

#region Timeline & Accounting DTOs

public class TimelineItemDto
{
    public string ItemType { get; set; } = "Activity"; // "Activity", "Untracked", "Future"
    public ulong? ActivityId { get; set; }
    public ulong? CategoryId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryGroup { get; set; } = "Other";
    public string Color { get; set; } = "#A8BDA5";
    public string Icon { get; set; } = "activity";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTimeLocal { get; set; }
    public DateTime EndTimeLocal { get; set; }
    public double DurationMinutes { get; set; }
    public bool IsRunning { get; set; }
    public StudySessionDetailDto? StudyDetails { get; set; }
}

public class DailyTimelineDto
{
    public DateOnly TargetDate { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public double TotalDayMinutes { get; set; } // Normally 1440.0
    public double TrackedMinutes { get; set; }
    public double UntrackedMinutes { get; set; }
    public double PlannedMinutes { get; set; }
    public IReadOnlyList<TimelineItemDto> TimelineItems { get; set; } = Array.Empty<TimelineItemDto>();
    public IReadOnlyList<PlannedActivityDto> PlannedItems { get; set; } = Array.Empty<PlannedActivityDto>();
}

public class CategoryTimeDistributionDto
{
    public string CategoryGroup { get; set; } = "Other";
    public double TotalMinutes { get; set; }
    public double PercentageOfDay { get; set; }
    public string Color { get; set; } = "#A8BDA5";
}

public class SubjectStudyTimeDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public double TotalMinutes { get; set; }
    public double TotalHours => Math.Round(TotalMinutes / 60.0, 1);
}

public class DailyTimeSummaryDto
{
    public DateOnly TargetDate { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public double TotalDayMinutes { get; set; }
    public double TrackedMinutes { get; set; }
    public double UntrackedMinutes { get; set; }
    public double StudyMinutes { get; set; }
    public double SleepMinutes { get; set; }
    public double MealMinutes { get; set; }
    public double WorkMinutes { get; set; }
    public double FitnessMinutes { get; set; }
    public double LeisureMinutes { get; set; }
    public double PersonalMinutes { get; set; }
    public double OtherMinutes { get; set; }
    public double PlannedMinutes { get; set; }
    public IReadOnlyList<CategoryTimeDistributionDto> Distribution { get; set; } = Array.Empty<CategoryTimeDistributionDto>();
    public IReadOnlyList<SubjectStudyTimeDto> SubjectStudyBreakdown { get; set; } = Array.Empty<SubjectStudyTimeDto>();
}

#endregion

#region Timer DTOs

public class ActiveTimerDto
{
    public bool HasActiveTimer { get; set; }
    public string? SessionGroupId { get; set; }
    public ulong? ActivityCategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public ulong? TopicId { get; set; }
    public string? TopicName { get; set; }
    public string? ModuleName { get; set; }
    public string? SubjectCode { get; set; }
    public string? Title { get; set; }
    public string? StudyMode { get; set; }
    public string TimerState { get; set; } = "Running"; // "Running" or "Paused"
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CurrentIntervalStartedAtUtc { get; set; }
    public double ElapsedSeconds { get; set; } // Server-calculated elapsed time (excluding paused gaps!)
}

public class StartTimerDto
{
    public ulong ActivityCategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ulong? TopicId { get; set; }
    public string? StudyMode { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
}

public class CompleteTimerDto
{
    public string? SessionNotes { get; set; }
    public uint QuestionsAttempted { get; set; }
    public uint QuestionsCorrect { get; set; }
    public string? DifficultyLevel { get; set; }
}

#endregion

#region Reports DTOs

public class DailyReportSummaryDto
{
    public DateOnly Date { get; set; }
    public double TotalTrackedHours { get; set; }
    public double StudyHours { get; set; }
    public double SleepHours { get; set; }
    public double UntrackedHours { get; set; }
}

public class WeeklyReportDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public double TotalTrackedHours { get; set; }
    public double TotalStudyHours { get; set; }
    public double TotalSleepHours { get; set; }
    public double AverageDailyStudyHours { get; set; }
    public double AverageDailySleepHours { get; set; }
    public IReadOnlyList<DailyReportSummaryDto> DailySummaries { get; set; } = Array.Empty<DailyReportSummaryDto>();
    public IReadOnlyList<CategoryTimeDistributionDto> CategoryDistribution { get; set; } = Array.Empty<CategoryTimeDistributionDto>();
    public IReadOnlyList<SubjectStudyTimeDto> SubjectBreakdown { get; set; } = Array.Empty<SubjectStudyTimeDto>();
}

public class MonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double TotalTrackedHours { get; set; }
    public double TotalStudyHours { get; set; }
    public double TotalSleepHours { get; set; }
    public double AverageDailyStudyHours { get; set; }
    public int TotalActiveStudyDays { get; set; }
    public IReadOnlyList<DailyReportSummaryDto> DailySummaries { get; set; } = Array.Empty<DailyReportSummaryDto>();
    public IReadOnlyList<CategoryTimeDistributionDto> CategoryDistribution { get; set; } = Array.Empty<CategoryTimeDistributionDto>();
    public IReadOnlyList<SubjectStudyTimeDto> SubjectBreakdown { get; set; } = Array.Empty<SubjectStudyTimeDto>();
}

#endregion

