using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SubjectPerformanceDto>> GetSubjectPerformanceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TopicPerformanceDto>> GetTopicPerformanceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<DailyStudyTrendDto>> GetDailyStudyTrendsAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TimeOfDayDistributionDto>> GetTimeOfDayDistributionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LifeCategoryDistributionDto>> GetLifeDistributionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<LifeAccountingDayDto> GetDailyLifeAccountingAsync(ulong userId, DateOnly? date, string? timezoneId, CancellationToken ct = default);
    Task<PlanAdherenceDto> GetPlanAdherenceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<QuestionPracticeSummaryDto> GetQuestionPracticeSummaryAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<MockTestAnalyticsDto> GetMockTestAnalyticsAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<RevisionRetentionDto> GetRevisionRetentionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<GoalAnalyticsDto>> GetGoalAnalyticsAsync(ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<HabitConsistencyDto>> GetHabitConsistencyAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<MindsetGrowthDto> GetMindsetGrowthAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<SmartRecommendationDto>> GetSmartRecommendationsAsync(ulong userId, string? timezoneId, CancellationToken ct = default);
}

