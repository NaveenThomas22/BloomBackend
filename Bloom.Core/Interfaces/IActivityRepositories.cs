using System.Data;
using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IActivityCategoryRepository
{
    Task<IReadOnlyList<ActivityCategory>> GetCategoriesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default);
    Task<ActivityCategory?> GetCategoryByIdAsync(ulong id, ulong? userId, CancellationToken ct = default);
    Task<ActivityCategory?> GetCategoryByCodeAsync(string code, ulong? userId, CancellationToken ct = default);
    Task<ActivityCategory> CreateCategoryAsync(ActivityCategory category, CancellationToken ct = default);
    Task<bool> UpdateCategoryAsync(ActivityCategory category, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<bool> IsCategoryUsedAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IActivityRecordRepository
{
    Task<ActivityRecord?> GetRecordByIdAsync(ulong id, ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityRecord>> GetRecordsByIntervalAsync(ulong userId, DateTime startUtc, DateTime endUtc, bool includeDeleted = false, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityRecord>> GetOverlappingRecordsAsync(ulong userId, DateTime startUtc, DateTime endUtc, ulong? excludeRecordId = null, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<ActivityRecord?> GetActiveRunningRecordAsync(ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityRecord>> GetRecordsBySessionGroupAsync(ulong userId, string sessionGroupId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<ActivityRecord> CreateRecordAsync(ActivityRecord record, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<bool> UpdateRecordAsync(ActivityRecord record, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<bool> SoftDeleteRecordAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IStudySessionDetailRepository
{
    Task<StudySessionDetail?> GetByActivityIdAsync(ulong activityId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<ulong, StudySessionDetail>> GetByActivityIdsAsync(IEnumerable<ulong> activityIds, CancellationToken ct = default);
    Task<StudySessionDetail> UpsertAsync(StudySessionDetail detail, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
}

public interface IPlannedActivityRepository
{
    Task<PlannedActivity?> GetByIdAsync(ulong id, ulong userId, CancellationToken ct = default);
    Task<IReadOnlyList<PlannedActivity>> GetPlannedByIntervalAsync(ulong userId, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<PlannedActivity> CreatePlannedActivityAsync(PlannedActivity plan, CancellationToken ct = default);
    Task<bool> UpdatePlannedActivityAsync(PlannedActivity plan, CancellationToken ct = default);
    Task<bool> DeletePlannedActivityAsync(ulong id, ulong userId, CancellationToken ct = default);
}

public interface IUserTimerStateRepository
{
    Task<UserTimerState?> GetByUserIdAsync(ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<UserTimerState> UpsertAsync(UserTimerState state, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
    Task<bool> DeleteAsync(ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default);
}

