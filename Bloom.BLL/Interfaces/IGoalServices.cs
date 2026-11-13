using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IRevisionPolicyService
{
    Task<IReadOnlyList<RevisionPolicyDto>> GetPoliciesAsync(ulong userId, CancellationToken ct = default);
    Task<RevisionPolicyDto> GetPolicyByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<RevisionPolicyDto> CreatePolicyAsync(ulong userId, CreateRevisionPolicyDto dto, CancellationToken ct = default);
    Task<RevisionPolicyDto> UpdatePolicyAsync(ulong id, ulong userId, UpdateRevisionPolicyDto dto, CancellationToken ct = default);
    Task DeletePolicyAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IRevisionScheduleService
{
    Task<RevisionScheduleDto?> GetScheduleByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<RevisionDashboardSummaryDto> GetDashboardSummaryAsync(ulong userId, DateOnly? date = null, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionScheduleDto>> GetUpcomingSchedulesAsync(ulong userId, int limit = 30, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionScheduleDto>> GetSchedulesForRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<RevisionScheduleDto> ScheduleInitialRevisionForTopicAsync(ulong userId, ulong topicId, ulong? policyId = null, DateOnly? fromDate = null, CancellationToken ct = default);
    Task<RevisionAttemptDto> RecordAttemptAsync(ulong userId, RecordRevisionAttemptRequest request, CancellationToken ct = default);
    Task<RevisionScheduleDto> RescheduleAsync(ulong id, ulong userId, RescheduleRevisionRequest request, CancellationToken ct = default);
    Task<RevisionScheduleDto> SkipAsync(ulong id, ulong userId, string? reason, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionAttemptDto>> GetAttemptsForTopicAsync(ulong userId, ulong topicId, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionAttemptDto>> GetRecentAttemptsAsync(ulong userId, int limit = 50, CancellationToken ct = default);
    Task<int> BatchUpdateStatusesAsync(ulong userId, DateOnly today, CancellationToken ct = default);
    Task<int> ReconcileMissingInitialSchedulesAsync(ulong? userId = null, CancellationToken ct = default);
}

public interface IGoalProgressService
{
    Task<decimal> CalculateCurrentProgressAsync(ulong userId, string targetMetric, DateOnly startDate, DateOnly endDate, ulong? subjectId, ulong? topicId, CancellationToken ct = default);
}

public interface IUserGoalService
{
    Task<IReadOnlyList<UserGoalDto>> GetGoalsAsync(ulong userId, string? period = null, string? status = null, CancellationToken ct = default);
    Task<UserGoalDto> GetGoalByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<GoalDashboardSummaryDto> GetDashboardAsync(ulong userId, string period, DateOnly? date = null, CancellationToken ct = default);
    Task<UserGoalDto> CreateGoalAsync(ulong userId, CreateGoalDto dto, CancellationToken ct = default);
    Task<UserGoalDto> UpdateGoalAsync(ulong id, ulong userId, UpdateGoalDto dto, CancellationToken ct = default);
    Task DeleteGoalAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IPreparationMilestoneService
{
    Task<IReadOnlyList<PreparationMilestoneDto>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default);
    Task<PreparationMilestoneDto> GetMilestoneByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<PreparationMilestoneDto> CreateMilestoneAsync(ulong userId, CreateMilestoneDto dto, CancellationToken ct = default);
    Task<PreparationMilestoneDto> UpdateMilestoneAsync(ulong id, ulong userId, UpdateMilestoneDto dto, CancellationToken ct = default);
    Task DeleteMilestoneAsync(ulong id, ulong userId, CancellationToken ct = default);
}

