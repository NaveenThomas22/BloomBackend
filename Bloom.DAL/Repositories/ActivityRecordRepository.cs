using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class ActivityRecordRepository : IActivityRecordRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ActivityRecordRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<ActivityRecord?> GetRecordByIdAsync(
        ulong id, 
        ulong userId, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                       activity_title AS ActivityTitle, description, start_at_utc AS StartAtUtc,
                       end_at_utc AS EndAtUtc, timezone_id AS TimezoneId, activity_status AS ActivityStatus,
                       entry_source AS EntrySource, is_planned AS IsPlanned, session_group_id AS SessionGroupId,
                       created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
                FROM activity_records
                WHERE id = @id AND user_id = @userId AND deleted_at_utc IS NULL;";

            return await conn.QuerySingleOrDefaultAsync<ActivityRecord>(new CommandDefinition(
                sql, new { id, userId }, transaction: transaction, cancellationToken: ct));
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetRecordsByIntervalAsync(
        ulong userId, 
        DateTime startUtc, 
        DateTime endUtc, 
        bool includeDeleted = false, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                   activity_title AS ActivityTitle, description, start_at_utc AS StartAtUtc,
                   end_at_utc AS EndAtUtc, timezone_id AS TimezoneId, activity_status AS ActivityStatus,
                   entry_source AS EntrySource, is_planned AS IsPlanned, session_group_id AS SessionGroupId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
            FROM activity_records
            WHERE user_id = @userId
              AND start_at_utc < @endUtc
              AND (end_at_utc IS NULL OR end_at_utc > @startUtc)
              AND (@includeDeleted = TRUE OR deleted_at_utc IS NULL)
              AND activity_status != 'Cancelled'
            ORDER BY start_at_utc ASC;";

        var results = await connection.QueryAsync<ActivityRecord>(new CommandDefinition(
            sql, new { userId, startUtc, endUtc, includeDeleted }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetOverlappingRecordsAsync(
        ulong userId, 
        DateTime startUtc, 
        DateTime endUtc, 
        ulong? excludeRecordId = null, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                       activity_title AS ActivityTitle, description, start_at_utc AS StartAtUtc,
                       end_at_utc AS EndAtUtc, timezone_id AS TimezoneId, activity_status AS ActivityStatus,
                       entry_source AS EntrySource, is_planned AS IsPlanned, session_group_id AS SessionGroupId,
                       created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
                FROM activity_records
                WHERE user_id = @userId
                  AND deleted_at_utc IS NULL
                  AND activity_status != 'Cancelled'
                  AND (@excludeRecordId IS NULL OR id != @excludeRecordId)
                  AND start_at_utc < @endUtc
                  AND (end_at_utc IS NULL OR end_at_utc > @startUtc)
                ORDER BY start_at_utc ASC;";

            var results = await conn.QueryAsync<ActivityRecord>(new CommandDefinition(
                sql, new { userId, startUtc, endUtc, excludeRecordId }, transaction: transaction, cancellationToken: ct));

            return results.AsList();
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<ActivityRecord?> GetActiveRunningRecordAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                   activity_title AS ActivityTitle, description, start_at_utc AS StartAtUtc,
                   end_at_utc AS EndAtUtc, timezone_id AS TimezoneId, activity_status AS ActivityStatus,
                   entry_source AS EntrySource, is_planned AS IsPlanned, session_group_id AS SessionGroupId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
            FROM activity_records
            WHERE user_id = @userId 
              AND activity_status = 'Running'
              AND end_at_utc IS NULL
              AND deleted_at_utc IS NULL
            ORDER BY start_at_utc DESC
            LIMIT 1;";

        return await connection.QuerySingleOrDefaultAsync<ActivityRecord>(new CommandDefinition(
            sql, new { userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ActivityRecord>> GetRecordsBySessionGroupAsync(
        ulong userId, 
        string sessionGroupId, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                       activity_title AS ActivityTitle, description, start_at_utc AS StartAtUtc,
                       end_at_utc AS EndAtUtc, timezone_id AS TimezoneId, activity_status AS ActivityStatus,
                       entry_source AS EntrySource, is_planned AS IsPlanned, session_group_id AS SessionGroupId,
                       created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
                FROM activity_records
                WHERE user_id = @userId 
                  AND session_group_id = @sessionGroupId
                  AND deleted_at_utc IS NULL
                  AND activity_status != 'Cancelled'
                ORDER BY start_at_utc ASC;";

            var results = await conn.QueryAsync<ActivityRecord>(new CommandDefinition(
                sql, new { userId, sessionGroupId }, transaction: transaction, cancellationToken: ct));

            return results.AsList();
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<ActivityRecord> CreateRecordAsync(
        ActivityRecord record, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                INSERT INTO activity_records 
                    (user_id, activity_category_id, activity_title, description, start_at_utc, end_at_utc, timezone_id, activity_status, entry_source, is_planned, session_group_id, created_at_utc, updated_at_utc)
                VALUES 
                    (@UserId, @ActivityCategoryId, @ActivityTitle, @Description, @StartAtUtc, @EndAtUtc, @TimezoneId, @ActivityStatus, @EntrySource, @IsPlanned, @SessionGroupId, UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await conn.ExecuteScalarAsync<ulong>(new CommandDefinition(
                sql, record, transaction: transaction, cancellationToken: ct));
            record.Id = id;
            return record;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<bool> UpdateRecordAsync(
        ActivityRecord record, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                UPDATE activity_records
                SET activity_category_id = @ActivityCategoryId,
                    activity_title = @ActivityTitle,
                    description = @Description,
                    start_at_utc = @StartAtUtc,
                    end_at_utc = @EndAtUtc,
                    timezone_id = @TimezoneId,
                    activity_status = @ActivityStatus,
                    entry_source = @EntrySource,
                    is_planned = @IsPlanned,
                    session_group_id = @SessionGroupId,
                    updated_at_utc = UTC_TIMESTAMP()
                WHERE id = @Id AND user_id = @UserId;";

            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql, record, transaction: transaction, cancellationToken: ct));
            return rows > 0;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<bool> SoftDeleteRecordAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE activity_records
            SET deleted_at_utc = UTC_TIMESTAMP(),
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @id AND user_id = @userId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }
}

