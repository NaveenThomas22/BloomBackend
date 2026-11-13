using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Enums;

namespace Bloom.Core.Interfaces;

public interface IRevisionPolicyRepository
{
    Task<IReadOnlyList<RevisionPolicy>> GetPoliciesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default);
    Task<RevisionPolicy?> GetPolicyByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<RevisionPolicy?> GetDefaultSystemPolicyAsync(CancellationToken ct = default);
    Task<RevisionPolicy> CreatePolicyAsync(RevisionPolicy policy, CancellationToken ct = default);
    Task<bool> UpdatePolicyAsync(RevisionPolicy policy, CancellationToken ct = default);
    Task<bool> DeletePolicyAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IRevisionScheduleRepository
{
    Task<RevisionSchedule?> GetScheduleByIdAsync(ulong id, ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionSchedule>> GetSchedulesByDateRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionSchedule>> GetDueAndOverdueSchedulesAsync(ulong userId, DateOnly today, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionSchedule>> GetUpcomingSchedulesAsync(ulong userId, DateOnly fromDate, int limit = 20, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionSchedule>> GetSchedulesByTopicIdAsync(ulong userId, ulong topicId, CancellationToken ct = default);
    Task<RevisionSchedule?> GetActiveScheduleForTopicAsync(ulong userId, ulong topicId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<RevisionSchedule> CreateScheduleAsync(RevisionSchedule schedule, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<bool> UpdateScheduleAsync(RevisionSchedule schedule, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<int> BatchUpdateStatusesAsync(ulong userId, DateOnly today, CancellationToken ct = default);
    Task<IReadOnlyList<(ulong UserId, ulong TopicId)>> GetCompletedTopicsMissingSchedulesAsync(ulong? userId = null, CancellationToken ct = default);
}

public interface IRevisionAttemptRepository
{
    Task<RevisionAttempt?> GetAttemptByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionAttempt>> GetAttemptsByTopicIdAsync(ulong userId, ulong topicId, CancellationToken ct = default);
    Task<IReadOnlyList<RevisionAttempt>> GetRecentAttemptsAsync(ulong userId, int limit = 50, CancellationToken ct = default);
    Task<RevisionAttempt> CreateAttemptAsync(RevisionAttempt attempt, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<int> GetCompletedAttemptsCountAsync(ulong userId, DateOnly startDate, DateOnly endDate, ulong? topicId = null, ulong? subjectId = null, CancellationToken ct = default);
}

public interface IUserGoalRepository
{
    Task<UserGoal?> GetGoalByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserGoal>> GetGoalsAsync(ulong userId, string? period = null, string? status = null, bool includeDeleted = false, CancellationToken ct = default);
    Task<IReadOnlyList<UserGoal>> GetActiveGoalsForDateAsync(ulong userId, DateOnly date, string? period = null, CancellationToken ct = default);
    Task<IReadOnlyList<UserGoal>> GetActiveGoalsForRangeAsync(ulong userId, DateOnly startDate, DateOnly endDate, string? period = null, CancellationToken ct = default);
    Task<UserGoal> CreateGoalAsync(UserGoal goal, CancellationToken ct = default);
    Task<bool> UpdateGoalAsync(UserGoal goal, CancellationToken ct = default);
    Task<bool> SoftDeleteGoalAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IPreparationMilestoneRepository
{
    Task<PreparationMilestone?> GetMilestoneByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<PreparationMilestone>> GetMilestonesAsync(ulong userId, string? status = null, bool includeDeleted = false, CancellationToken ct = default);
    Task<PreparationMilestone> CreateMilestoneAsync(PreparationMilestone milestone, CancellationToken ct = default);
    Task<bool> UpdateMilestoneAsync(PreparationMilestone milestone, CancellationToken ct = default);
    Task<bool> SoftDeleteMilestoneAsync(ulong id, ulong userId, CancellationToken ct = default);
}

