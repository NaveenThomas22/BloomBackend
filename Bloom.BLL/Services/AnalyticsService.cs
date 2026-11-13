using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bloom.BLL.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        ILogger<AnalyticsService> logger)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetOverviewMetricsAsync(userId, startUtc, endUtc, ct);

        return new AnalyticsOverviewDto
        {
            StudyHoursPeriod = data.StudyHoursPeriod,
            StudyHoursAllTime = data.StudyHoursAllTime,
            QuestionsAttempted = data.QuestionsAttempted,
            QuestionsCorrect = data.QuestionsCorrect,
            PracticeAccuracy = data.PracticeAccuracy,
            TopicsMastered = data.TopicsMastered,
            TopicsCompleted = data.TopicsCompleted,
            TotalTopics = data.TotalTopics,
            MockTestsCompleted = data.MockTestsCompleted,
            AverageMockScore = data.AverageMockScore,
            CurrentStreakDays = data.CurrentStreakDays,
            HasActiveTimer = data.HasActiveTimer,
            ActiveTimerMinutes = data.ActiveTimerMinutes
        };
    }

    public async Task<IReadOnlyList<SubjectPerformanceDto>> GetSubjectPerformanceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetSubjectPerformanceAsync(userId, startUtc, endUtc, ct);

        return data.Select(x => new SubjectPerformanceDto
        {
            SubjectId = x.SubjectId,
            SubjectCode = x.SubjectCode,
            SubjectName = x.SubjectName,
            TotalTopics = x.TotalTopics,
            CompletedTopics = x.CompletedTopics,
            MasteredTopics = x.MasteredTopics,
            StudyHours = x.StudyHours,
            QuestionsAttempted = x.QuestionsAttempted,
            QuestionsCorrect = x.QuestionsCorrect,
            AccuracyPercentage = x.AccuracyPercentage,
            AverageTimeSpentSeconds = x.AverageTimeSpentSeconds
        }).ToList();
    }

    public async Task<IReadOnlyList<TopicPerformanceDto>> GetTopicPerformanceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetTopicPerformanceListAsync(userId, request.SubjectId, startUtc, endUtc, ct);

        return data.Select(x => new TopicPerformanceDto
        {
            TopicId = x.TopicId,
            TopicName = x.TopicName,
            SubjectCode = x.SubjectCode,
            SubjectName = x.SubjectName,
            ModuleName = x.ModuleName,
            Status = x.Status,
            ConfidenceRating = x.ConfidenceRating,
            QuestionsAttempted = x.QuestionsAttempted,
            QuestionsCorrect = x.QuestionsCorrect,
            AccuracyPercentage = x.AccuracyPercentage,
            StudyHours = x.StudyHours
        }).ToList();
    }

    public async Task<IReadOnlyList<DailyStudyTrendDto>> GetDailyStudyTrendsAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var sUtc = startUtc ?? DateTime.UtcNow.AddDays(-7);
        var eUtc = endUtc ?? DateTime.UtcNow;

        var data = await _analyticsRepository.GetDailyStudyTimeTrendsAsync(userId, sUtc, eUtc, request.TimezoneId, ct);

        return data.Select(x => new DailyStudyTrendDto
        {
            Date = x.Date,
            StudyHours = x.StudyHours,
            QaHours = x.QaHours,
            DilrHours = x.DilrHours,
            VarcHours = x.VarcHours,
            OtherHours = x.OtherHours
        }).ToList();
    }

    public async Task<IReadOnlyList<TimeOfDayDistributionDto>> GetTimeOfDayDistributionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetTimeOfDayDistributionAsync(userId, startUtc, endUtc, request.TimezoneId, ct);

        return data.Select(x => new TimeOfDayDistributionDto
        {
            TimeBlock = x.TimeBlock,
            StudyHours = x.StudyHours,
            ActivityCount = x.ActivityCount
        }).ToList();
    }

    public async Task<IReadOnlyList<LifeCategoryDistributionDto>> GetLifeDistributionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetLifeCategoryDistributionAsync(userId, startUtc, endUtc, ct);

        return data.Select(x => new LifeCategoryDistributionDto
        {
            CategoryGroup = x.CategoryGroup,
            CategoryName = x.CategoryName,
            Color = x.Color,
            TotalHours = x.TotalHours,
            Percentage = x.Percentage
        }).ToList();
    }

    public async Task<LifeAccountingDayDto> GetDailyLifeAccountingAsync(ulong userId, DateOnly? date, string? timezoneId, CancellationToken ct = default)
    {
        var tz = timezoneId ?? "Asia/Kolkata";
        var localDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var data = await _analyticsRepository.GetDailyLifeAccountingAsync(userId, localDate, tz, ct);

        return new LifeAccountingDayDto
        {
            Date = data.Date,
            AccountedHours = data.AccountedHours,
            UntrackedHours = data.UntrackedHours,
            CalendarDayHours = data.CalendarDayHours
        };
    }

    public async Task<PlanAdherenceDto> GetPlanAdherenceAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetPlanAdherenceAsync(userId, startUtc, endUtc, ct);

        return new PlanAdherenceDto
        {
            TotalPlannedHours = data.TotalPlannedHours,
            TotalActualHours = data.TotalActualHours,
            CompletedPlansCount = data.CompletedPlansCount,
            TotalScheduledPlansCount = data.TotalScheduledPlansCount,
            AdherenceRatePercentage = data.AdherenceRatePercentage
        };
    }

    public async Task<QuestionPracticeSummaryDto> GetQuestionPracticeSummaryAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetQuestionPracticeSummaryAsync(userId, request.SubjectId, request.TopicId, startUtc, endUtc, ct);

        return new QuestionPracticeSummaryDto
        {
            TotalSessions = data.TotalSessions,
            TotalQuestionsAttempted = data.TotalQuestionsAttempted,
            TotalQuestionsCorrect = data.TotalQuestionsCorrect,
            OverallAccuracy = data.OverallAccuracy,
            McqAttempted = data.McqAttempted,
            McqCorrect = data.McqCorrect,
            McqAccuracy = data.McqAccuracy,
            TitaAttempted = data.TitaAttempted,
            TitaCorrect = data.TitaCorrect,
            TitaAccuracy = data.TitaAccuracy,
            EasyAccuracy = data.EasyAccuracy,
            MediumAccuracy = data.MediumAccuracy,
            HardAccuracy = data.HardAccuracy,
            AvgSecondsPerQuestion = data.AvgSecondsPerQuestion
        };
    }

    public async Task<MockTestAnalyticsDto> GetMockTestAnalyticsAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetMockTestHistoryAsync(userId, startUtc, endUtc, ct);

        var items = data.Select(x => new MockTestSummaryItemDto
        {
            AttemptId = x.AttemptId,
            ExamTemplateId = x.ExamTemplateId,
            ExamTitle = x.ExamTitle,
            ExamType = x.ExamType,
            SubmittedAtUtc = x.SubmittedAtUtc,
            TotalScore = x.TotalScore,
            MaxPossibleScore = x.MaxPossibleScore,
            AccuracyPercentage = x.AccuracyPercentage,
            TotalAttempted = x.TotalAttempted,
            TotalCorrect = x.TotalCorrect,
            TotalIncorrect = x.TotalIncorrect,
            TotalUnanswered = x.TotalUnanswered,
            TimeSpentSeconds = x.TimeSpentSeconds,
            SectionScores = x.SectionScores.Select(s => new MockSectionScoreDto
            {
                SectionName = s.SectionName,
                Score = s.Score,
                MaxScore = s.MaxScore,
                Attempted = s.Attempted,
                Correct = s.Correct,
                AccuracyPercentage = s.AccuracyPercentage,
                TimeSpentSeconds = s.TimeSpentSeconds
            }).ToList()
        }).ToList();

        decimal avgScore = items.Count > 0 ? Math.Round(items.Average(i => i.TotalScore), 2) : 0m;

        return new MockTestAnalyticsDto
        {
            TotalCompletedMocks = items.Count,
            AverageScore = avgScore,
            CohortStatus = "Cohort percentile unavailable (Single-user mode)",
            Attempts = items
        };
    }

    public async Task<RevisionRetentionDto> GetRevisionRetentionAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetRevisionRetentionSummaryAsync(userId, startUtc, endUtc, ct);

        return new RevisionRetentionDto
        {
            TotalSchedules = data.TotalSchedules,
            CompletedSchedules = data.CompletedSchedules,
            OverdueSchedules = data.OverdueSchedules,
            UpcomingSchedules = data.UpcomingSchedules,
            CompletionRatePercentage = data.CompletionRatePercentage,
            AverageConfidence = data.AverageConfidence,
            UnderstandingDistribution = data.UnderstandingDistribution
        };
    }

    public async Task<IReadOnlyList<GoalAnalyticsDto>> GetGoalAnalyticsAsync(ulong userId, CancellationToken ct = default)
    {
        var data = await _analyticsRepository.GetGoalAnalyticsAsync(userId, ct);

        return data.Select(x => new GoalAnalyticsDto
        {
            GoalId = x.GoalId,
            Title = x.Title,
            GoalPeriod = x.GoalPeriod,
            TargetMetric = x.TargetMetric,
            TargetValue = x.TargetValue,
            CurrentValue = x.CurrentValue,
            Unit = x.Unit,
            ProgressPercentage = x.ProgressPercentage,
            IsCompleted = x.IsCompleted
        }).ToList();
    }

    public async Task<IReadOnlyList<HabitConsistencyDto>> GetHabitConsistencyAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetHabitConsistencyAsync(userId, startUtc, endUtc, ct);

        return data.Select(x => new HabitConsistencyDto
        {
            HabitId = x.HabitId,
            HabitName = x.HabitName,
            CategoryName = x.CategoryName,
            Frequency = x.Frequency,
            CurrentStreak = x.CurrentStreak,
            LongestStreak = x.LongestStreak,
            CompletionRatePercentage = x.CompletionRatePercentage,
            TotalCompleted = x.TotalCompleted,
            TotalSkipped = x.TotalSkipped,
            TotalMissed = x.TotalMissed
        }).ToList();
    }

    public async Task<MindsetGrowthDto> GetMindsetGrowthAsync(ulong userId, AnalyticsFilterRequest request, CancellationToken ct = default)
    {
        var (startUtc, endUtc) = ResolveDateRangeUtc(request);
        var data = await _analyticsRepository.GetMindsetGrowthDataAsync(userId, startUtc, endUtc, ct);

        return new MindsetGrowthDto
        {
            MorningReflectionsCount = data.MorningReflectionsCount,
            EveningReflectionsCount = data.EveningReflectionsCount,
            TotalDaysWithReflections = data.TotalDaysWithReflections,
            AverageMorningEnergy = data.AverageMorningEnergy,
            AverageEveningSatisfaction = data.AverageEveningSatisfaction,
            TotalPlannedStudyMinutes = data.TotalPlannedStudyMinutes,
            ActualStudyMinutes = data.ActualStudyMinutes,
            MoodDistribution = data.MoodDistribution.Select(m => new MoodCountDto
            {
                MoodCode = m.MoodCode,
                MoodName = m.MoodName,
                Emoji = m.Emoji,
                Color = m.Color,
                Count = m.Count,
                Percentage = m.Percentage
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<SmartRecommendationDto>> GetSmartRecommendationsAsync(ulong userId, string? timezoneId, CancellationToken ct = default)
    {
        var tz = timezoneId ?? "Asia/Kolkata";
        var recommendations = new List<SmartRecommendationDto>();

        try
        {
            // 1. Weak Topic Rule: Any topic with >= 3 attempts and accuracy < 55%
            var topics = await _analyticsRepository.GetTopicPerformanceListAsync(userId, null, null, null, ct);
            var weakTopics = topics
                .Where(t => t.QuestionsAttempted >= 3 && t.AccuracyPercentage < 55m)
                .OrderBy(t => t.AccuracyPercentage)
                .Take(2)
                .ToList();

            foreach (var wt in weakTopics)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    Id = $"REC_WEAK_{wt.TopicId}",
                    Category = "Practice",
                    Priority = "Critical",
                    Title = $"Targeted Practice: {wt.TopicName}",
                    Message = $"Your practice accuracy in {wt.TopicName} ({wt.SubjectCode}) is {wt.AccuracyPercentage}%. Focused revision will turn this into a strong area.",
                    ActionLabel = "Practice Questions",
                    ActionUrl = $"/practice?topicId={wt.TopicId}"
                });
            }

            // 2. Overdue Revision Rule: Any active revision schedules marked Overdue
            var revStats = await _analyticsRepository.GetRevisionRetentionSummaryAsync(userId, null, null, ct);
            if (revStats.OverdueSchedules > 0)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    Id = "REC_OVERDUE_REVISION",
                    Category = "Revision",
                    Priority = "Critical",
                    Title = "Spaced Repetition Overdue",
                    Message = $"You have {revStats.OverdueSchedules} scheduled revision(s) overdue. Reviewing on time prevents memory decay.",
                    ActionLabel = "View Revisions",
                    ActionUrl = "/revisions"
                });
            }

            // 3. CAT Subject Imbalance Rule: Study hours in the last 14 days
            var twoWeeksAgoUtc = DateTime.UtcNow.AddDays(-14);
            var subjectStats = await _analyticsRepository.GetSubjectPerformanceAsync(userId, twoWeeksAgoUtc, DateTime.UtcNow, ct);
            decimal totalCatHours = subjectStats.Sum(s => s.StudyHours);

            if (totalCatHours >= 5.0m)
            {
                foreach (var s in subjectStats)
                {
                    decimal pct = totalCatHours > 0 ? (s.StudyHours / totalCatHours * 100m) : 0m;
                    if (pct < 15.0m)
                    {
                        recommendations.Add(new SmartRecommendationDto
                        {
                            Id = $"REC_IMBALANCE_{s.SubjectCode}",
                            Category = "Study",
                            Priority = "Important",
                            Title = $"Balance Your Preparation: {s.SubjectCode}",
                            Message = $"{s.SubjectName} accounts for only {Math.Round(pct, 1)}% of your study time in the last 14 days. Balanced preparation across QA, DILR, and VARC is vital for CAT.",
                            ActionLabel = $"Explore {s.SubjectCode}",
                            ActionUrl = $"/syllabus?subject={s.SubjectCode}"
                        });
                    }
                }
            }

            // 4. Mock Test Stagnation Rule: Check if a mock test was completed in the last 14 days
            var mockStats = await _analyticsRepository.GetMockTestHistoryAsync(userId, twoWeeksAgoUtc, DateTime.UtcNow, ct);
            if (mockStats.Count == 0)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    Id = "REC_MOCK_STAGNATION",
                    Category = "Practice",
                    Priority = "Important",
                    Title = "Take a Mock Examination",
                    Message = "You haven't attempted a full or sectional mock test in the past 14 days. Consistent mock tests train time management and test stamina.",
                    ActionLabel = "Go to Mock Engine",
                    ActionUrl = "/mocks"
                });
            }

            // 5. Habit Streak Milestone Rule: Active habit with streak >= 7
            var habits = await _analyticsRepository.GetHabitConsistencyAsync(userId, null, null, ct);
            var highStreakHabit = habits.FirstOrDefault(h => h.CurrentStreak >= 7);
            if (highStreakHabit != null)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    Id = $"REC_STREAK_{highStreakHabit.HabitId}",
                    Category = "Habit",
                    Priority = "Informational",
                    Title = "Remarkable Consistency!",
                    Message = $"You have maintained a {highStreakHabit.CurrentStreak}-day streak on '{highStreakHabit.HabitName}'. Consistent daily habits build CAT mastery.",
                    ActionLabel = "View Habits",
                    ActionUrl = "/habits"
                });
            }

            // 6. Active Goal Pace Rule: Check active goals
            var goals = await _analyticsRepository.GetGoalAnalyticsAsync(userId, ct);
            var laggingGoal = goals.FirstOrDefault(g => !g.IsCompleted && g.ProgressPercentage < 60m && g.GoalPeriod == "Weekly");
            if (laggingGoal != null)
            {
                recommendations.Add(new SmartRecommendationDto
                {
                    Id = $"REC_GOAL_PACE_{laggingGoal.GoalId}",
                    Category = "Goals",
                    Priority = "Important",
                    Title = $"Goal Target: {laggingGoal.Title}",
                    Message = $"Your weekly goal '{laggingGoal.Title}' is currently at {Math.Round(laggingGoal.ProgressPercentage, 0)}% ({laggingGoal.CurrentValue} / {laggingGoal.TargetValue} {laggingGoal.Unit}). Keep going!",
                    ActionLabel = "Review Goals",
                    ActionUrl = "/goals"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating smart recommendations for user {UserId}", userId);
        }

        return recommendations;
    }

    private static (DateTime? startUtc, DateTime? endUtc) ResolveDateRangeUtc(AnalyticsFilterRequest request)
    {
        if (string.Equals(request.TimeRange, "AllTime", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        TimeZoneInfo userTz;
        try
        {
            userTz = TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneId);
        }
        catch
        {
            userTz = TimeZoneInfo.Utc;
        }

        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, userTz);
        var todayLocal = DateOnly.FromDateTime(nowLocal);

        switch (request.TimeRange?.ToLowerInvariant())
        {
            case "today":
                return (
                    TimeZoneInfo.ConvertTimeToUtc(todayLocal.ToDateTime(TimeOnly.MinValue), userTz),
                    TimeZoneInfo.ConvertTimeToUtc(todayLocal.ToDateTime(TimeOnly.MaxValue), userTz)
                );

            case "last7days":
                var sevenDaysAgo = todayLocal.AddDays(-6);
                return (
                    TimeZoneInfo.ConvertTimeToUtc(sevenDaysAgo.ToDateTime(TimeOnly.MinValue), userTz),
                    TimeZoneInfo.ConvertTimeToUtc(todayLocal.ToDateTime(TimeOnly.MaxValue), userTz)
                );

            case "last30days":
                var thirtyDaysAgo = todayLocal.AddDays(-29);
                return (
                    TimeZoneInfo.ConvertTimeToUtc(thirtyDaysAgo.ToDateTime(TimeOnly.MinValue), userTz),
                    TimeZoneInfo.ConvertTimeToUtc(todayLocal.ToDateTime(TimeOnly.MaxValue), userTz)
                );

            case "thismonth":
                var firstDayThisMonth = new DateOnly(todayLocal.Year, todayLocal.Month, 1);
                var lastDayThisMonth = firstDayThisMonth.AddMonths(1).AddDays(-1);
                return (
                    TimeZoneInfo.ConvertTimeToUtc(firstDayThisMonth.ToDateTime(TimeOnly.MinValue), userTz),
                    TimeZoneInfo.ConvertTimeToUtc(lastDayThisMonth.ToDateTime(TimeOnly.MaxValue), userTz)
                );

            case "lastmonth":
                var firstDayLastMonth = new DateOnly(todayLocal.Year, todayLocal.Month, 1).AddMonths(-1);
                var lastDayLastMonth = firstDayLastMonth.AddMonths(1).AddDays(-1);
                return (
                    TimeZoneInfo.ConvertTimeToUtc(firstDayLastMonth.ToDateTime(TimeOnly.MinValue), userTz),
                    TimeZoneInfo.ConvertTimeToUtc(lastDayLastMonth.ToDateTime(TimeOnly.MaxValue), userTz)
                );

            case "custom":
                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    return (
                        TimeZoneInfo.ConvertTimeToUtc(request.StartDate.Value.ToDateTime(TimeOnly.MinValue), userTz),
                        TimeZoneInfo.ConvertTimeToUtc(request.EndDate.Value.ToDateTime(TimeOnly.MaxValue), userTz)
                    );
                }
                break;
        }

        // Default: Last 7 days
        var defaultStart = todayLocal.AddDays(-6);
        return (
            TimeZoneInfo.ConvertTimeToUtc(defaultStart.ToDateTime(TimeOnly.MinValue), userTz),
            TimeZoneInfo.ConvertTimeToUtc(todayLocal.ToDateTime(TimeOnly.MaxValue), userTz)
        );
    }
}

