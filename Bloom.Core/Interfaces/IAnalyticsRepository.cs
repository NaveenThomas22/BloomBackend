using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task<AnalyticsOverviewData> GetOverviewMetricsAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectPerformanceData>> GetSubjectPerformanceAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<TopicPerformanceData>> GetTopicPerformanceListAsync(ulong userId, ulong? subjectId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<DailyStudyTimeData>> GetDailyStudyTimeTrendsAsync(ulong userId, DateTime startUtc, DateTime endUtc, string timezoneId, CancellationToken ct = default);
    Task<IReadOnlyList<TimeOfDayDistributionData>> GetTimeOfDayDistributionAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, string timezoneId, CancellationToken ct = default);
    Task<IReadOnlyList<LifeCategoryDistributionData>> GetLifeCategoryDistributionAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<LifeAccountingDayData> GetDailyLifeAccountingAsync(ulong userId, DateOnly localDate, string timezoneId, CancellationToken ct = default);
    Task<PlanAdherenceData> GetPlanAdherenceAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<QuestionPracticeSummaryData> GetQuestionPracticeSummaryAsync(ulong userId, ulong? subjectId, ulong? topicId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<MockTestSummaryItem>> GetMockTestHistoryAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<RevisionRetentionData> GetRevisionRetentionSummaryAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<GoalAnalyticsItem>> GetGoalAnalyticsAsync(ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitConsistencyItem>> GetHabitConsistencyAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
    Task<MindsetGrowthData> GetMindsetGrowthDataAsync(ulong userId, DateTime? startUtc, DateTime? endUtc, CancellationToken ct = default);
}

