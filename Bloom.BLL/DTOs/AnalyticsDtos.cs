namespace Bloom.BLL.DTOs;

public class AnalyticsFilterRequest
{
    public string TimeRange { get; set; } = "Last7Days"; // Today, Last7Days, Last30Days, ThisMonth, LastMonth, Custom, AllTime
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string TimezoneId { get; set; } = "Asia/Kolkata";
    public ulong? SubjectId { get; set; }
    public ulong? TopicId { get; set; }
}

public class AnalyticsOverviewDto
{
    public decimal StudyHoursPeriod { get; set; }
    public decimal StudyHoursAllTime { get; set; }
    public int QuestionsAttempted { get; set; }
    public int QuestionsCorrect { get; set; }
    public decimal PracticeAccuracy { get; set; }
    public int TopicsMastered { get; set; }
    public int TopicsCompleted { get; set; }
    public int TotalTopics { get; set; }
    public int MockTestsCompleted { get; set; }
    public decimal AverageMockScore { get; set; }
    public int CurrentStreakDays { get; set; }
    public bool HasActiveTimer { get; set; }
    public int ActiveTimerMinutes { get; set; }
}

public class SubjectPerformanceDto
{
    public ulong SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int TotalTopics { get; set; }
    public int CompletedTopics { get; set; }
    public int MasteredTopics { get; set; }
    public decimal StudyHours { get; set; }
    public int QuestionsAttempted { get; set; }
    public int QuestionsCorrect { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal AverageTimeSpentSeconds { get; set; }
}

public class TopicPerformanceDto
{
    public ulong TopicId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Status { get; set; } = "NotStarted";
    public int ConfidenceRating { get; set; }
    public int QuestionsAttempted { get; set; }
    public int QuestionsCorrect { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public decimal StudyHours { get; set; }
}

public class DailyStudyTrendDto
{
    public DateOnly Date { get; set; }
    public decimal StudyHours { get; set; }
    public decimal QaHours { get; set; }
    public decimal DilrHours { get; set; }
    public decimal VarcHours { get; set; }
    public decimal OtherHours { get; set; }
}

public class TimeOfDayDistributionDto
{
    public string TimeBlock { get; set; } = string.Empty;
    public decimal StudyHours { get; set; }
    public int ActivityCount { get; set; }
}

public class LifeCategoryDistributionDto
{
    public string CategoryGroup { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Color { get; set; } = "#A8BDA5";
    public decimal TotalHours { get; set; }
    public decimal Percentage { get; set; }
}

public class LifeAccountingDayDto
{
    public DateOnly Date { get; set; }
    public decimal AccountedHours { get; set; }
    public decimal UntrackedHours { get; set; }
    public decimal CalendarDayHours { get; set; }
}

public class PlanAdherenceDto
{
    public decimal TotalPlannedHours { get; set; }
    public decimal TotalActualHours { get; set; }
    public int CompletedPlansCount { get; set; }
    public int TotalScheduledPlansCount { get; set; }
    public decimal AdherenceRatePercentage { get; set; }
}

public class QuestionPracticeSummaryDto
{
    public int TotalSessions { get; set; }
    public int TotalQuestionsAttempted { get; set; }
    public int TotalQuestionsCorrect { get; set; }
    public decimal OverallAccuracy { get; set; }
    public int McqAttempted { get; set; }
    public int McqCorrect { get; set; }
    public decimal McqAccuracy { get; set; }
    public int TitaAttempted { get; set; }
    public int TitaCorrect { get; set; }
    public decimal TitaAccuracy { get; set; }
    public decimal EasyAccuracy { get; set; }
    public decimal MediumAccuracy { get; set; }
    public decimal HardAccuracy { get; set; }
    public decimal AvgSecondsPerQuestion { get; set; }
}

public class MockTestAnalyticsDto
{
    public int TotalCompletedMocks { get; set; }
    public decimal AverageScore { get; set; }
    public string CohortStatus { get; set; } = "Cohort percentile unavailable (Single-user mode)";
    public List<MockTestSummaryItemDto> Attempts { get; set; } = new();
}

public class MockTestSummaryItemDto
{
    public ulong AttemptId { get; set; }
    public ulong ExamTemplateId { get; set; }
    public string ExamTitle { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TotalAttempted { get; set; }
    public int TotalCorrect { get; set; }
    public int TotalIncorrect { get; set; }
    public int TotalUnanswered { get; set; }
    public int TimeSpentSeconds { get; set; }
    public List<MockSectionScoreDto> SectionScores { get; set; } = new();
}

public class MockSectionScoreDto
{
    public string SectionName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public int Attempted { get; set; }
    public int Correct { get; set; }
    public decimal AccuracyPercentage { get; set; }
    public int TimeSpentSeconds { get; set; }
}

public class RevisionRetentionDto
{
    public int TotalSchedules { get; set; }
    public int CompletedSchedules { get; set; }
    public int OverdueSchedules { get; set; }
    public int UpcomingSchedules { get; set; }
    public decimal CompletionRatePercentage { get; set; }
    public decimal AverageConfidence { get; set; }
    public Dictionary<string, int> UnderstandingDistribution { get; set; } = new();
}

public class GoalAnalyticsDto
{
    public ulong GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string GoalPeriod { get; set; } = string.Empty;
    public string TargetMetric { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal ProgressPercentage { get; set; }
    public bool IsCompleted { get; set; }
}

public class HabitConsistencyDto
{
    public ulong HabitId { get; set; }
    public string HabitName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public decimal CompletionRatePercentage { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalMissed { get; set; }
}

public class MindsetGrowthDto
{
    public int MorningReflectionsCount { get; set; }
    public int EveningReflectionsCount { get; set; }
    public int TotalDaysWithReflections { get; set; }
    public decimal? AverageMorningEnergy { get; set; }
    public decimal? AverageEveningSatisfaction { get; set; }
    public int TotalPlannedStudyMinutes { get; set; }
    public int ActualStudyMinutes { get; set; }
    public List<MoodCountDto> MoodDistribution { get; set; } = new();
}

public class MoodCountDto
{
    public string MoodCode { get; set; } = string.Empty;
    public string MoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public string Color { get; set; } = "#A8BDA5";
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

public class SmartRecommendationDto
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Study, Practice, Revision, Habit, Wellness, Goals
    public string Priority { get; set; } = "Important"; // Critical, Important, Informational
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
}

