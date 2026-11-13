using Bloom.BLL.DTOs;
using Bloom.BLL.Interfaces;
using Bloom.Core.Interfaces;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[Authorize]
[Route("api/v1/analytics")]
public class AnalyticsController : BaseApiController
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ICurrentUserService _currentUserService;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ICurrentUserService currentUserService)
    {
        _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ApiResponse<AnalyticsOverviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AnalyticsOverviewDto>>> GetOverview(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetOverviewAsync(userId, request, ct);
        return Success(result, "Analytics overview retrieved successfully.");
    }

    [HttpGet("subject-performance")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SubjectPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubjectPerformanceDto>>>> GetSubjectPerformance(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetSubjectPerformanceAsync(userId, request, ct);
        return Success(result, "Subject performance retrieved successfully.");
    }

    [HttpGet("topic-performance")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TopicPerformanceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopicPerformanceDto>>>> GetTopicPerformance(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetTopicPerformanceAsync(userId, request, ct);
        return Success(result, "Topic performance retrieved successfully.");
    }

    [HttpGet("study-time-trends")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DailyStudyTrendDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DailyStudyTrendDto>>>> GetStudyTrends(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetDailyStudyTrendsAsync(userId, request, ct);
        return Success(result, "Daily study trends retrieved successfully.");
    }

    [HttpGet("time-of-day")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeOfDayDistributionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TimeOfDayDistributionDto>>>> GetTimeOfDay(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetTimeOfDayDistributionAsync(userId, request, ct);
        return Success(result, "Time of day distribution retrieved successfully.");
    }

    [HttpGet("life-distribution")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LifeCategoryDistributionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LifeCategoryDistributionDto>>>> GetLifeDistribution(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetLifeDistributionAsync(userId, request, ct);
        return Success(result, "Life category distribution retrieved successfully.");
    }

    [HttpGet("life-accounting-day")]
    [ProducesResponseType(typeof(ApiResponse<LifeAccountingDayDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LifeAccountingDayDto>>> GetDailyLifeAccounting(
        [FromQuery] DateOnly? date,
        [FromQuery] string? timezoneId,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetDailyLifeAccountingAsync(userId, date, timezoneId, ct);
        return Success(result, "Daily life accounting retrieved successfully.");
    }

    [HttpGet("plan-adherence")]
    [ProducesResponseType(typeof(ApiResponse<PlanAdherenceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PlanAdherenceDto>>> GetPlanAdherence(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetPlanAdherenceAsync(userId, request, ct);
        return Success(result, "Plan adherence metrics retrieved successfully.");
    }

    [HttpGet("question-practice")]
    [ProducesResponseType(typeof(ApiResponse<QuestionPracticeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<QuestionPracticeSummaryDto>>> GetQuestionPractice(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetQuestionPracticeSummaryAsync(userId, request, ct);
        return Success(result, "Question practice summary retrieved successfully.");
    }

    [HttpGet("mock-tests")]
    [ProducesResponseType(typeof(ApiResponse<MockTestAnalyticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MockTestAnalyticsDto>>> GetMockTests(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetMockTestAnalyticsAsync(userId, request, ct);
        return Success(result, "Mock test analytics retrieved successfully.");
    }

    [HttpGet("revisions")]
    [ProducesResponseType(typeof(ApiResponse<RevisionRetentionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RevisionRetentionDto>>> GetRevisions(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetRevisionRetentionAsync(userId, request, ct);
        return Success(result, "Revision retention metrics retrieved successfully.");
    }

    [HttpGet("goals")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GoalAnalyticsDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GoalAnalyticsDto>>>> GetGoals(
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetGoalAnalyticsAsync(userId, ct);
        return Success(result, "Goal analytics retrieved successfully.");
    }

    [HttpGet("habits")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HabitConsistencyDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HabitConsistencyDto>>>> GetHabits(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetHabitConsistencyAsync(userId, request, ct);
        return Success(result, "Habit consistency metrics retrieved successfully.");
    }

    [HttpGet("mindset-growth")]
    [ProducesResponseType(typeof(ApiResponse<MindsetGrowthDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MindsetGrowthDto>>> GetMindsetGrowth(
        [FromQuery] AnalyticsFilterRequest request,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetMindsetGrowthAsync(userId, request, ct);
        return Success(result, "Mindset and personal growth analytics retrieved successfully.");
    }

    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SmartRecommendationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SmartRecommendationDto>>>> GetRecommendations(
        [FromQuery] string? timezoneId,
        CancellationToken ct)
    {
        var userId = _currentUserService.GetRequiredUserId();
        var result = await _analyticsService.GetSmartRecommendationsAsync(userId, timezoneId, ct);
        return Success(result, "Smart recommendations generated successfully.");
    }
}

