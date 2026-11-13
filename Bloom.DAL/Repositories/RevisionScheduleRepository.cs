
using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class RevisionScheduleRepository : IRevisionScheduleRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RevisionScheduleRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<RevisionSchedule?> GetScheduleByIdAsync(
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
                SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                       step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                       completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                       created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
                FROM revision_schedules
                WHERE id = @id AND user_id = @userId AND is_deleted = FALSE;";

            return await conn.QuerySingleOrDefaultAsync<RevisionSchedule>(new CommandDefinition(
                sql, new { id, userId }, transaction: transaction, cancellationToken: ct));
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<IReadOnlyList<RevisionSchedule>> GetSchedulesByDateRangeAsync(
        ulong userId, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                   step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_schedules
            WHERE user_id = @userId
              AND scheduled_date >= @startDate
              AND scheduled_date <= @endDate
              AND is_deleted = FALSE
            ORDER BY scheduled_date ASC, step_number ASC;";

        var results = await connection.QueryAsync<RevisionSchedule>(new CommandDefinition(
            sql, new { userId, startDate = startDate.ToString("yyyy-MM-dd"), endDate = endDate.ToString("yyyy-MM-dd") }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<RevisionSchedule>> GetDueAndOverdueSchedulesAsync(
        ulong userId, 
        DateOnly today, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                   step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_schedules
            WHERE user_id = @userId
              AND scheduled_date <= @today
              AND status IN ('Upcoming', 'DueToday', 'Overdue')
              AND is_deleted = FALSE
            ORDER BY scheduled_date ASC, step_number ASC;";

        var results = await connection.QueryAsync<RevisionSchedule>(new CommandDefinition(
            sql, new { userId, today = today.ToString("yyyy-MM-dd") }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<RevisionSchedule>> GetUpcomingSchedulesAsync(
        ulong userId, 
        DateOnly fromDate, 
        int limit = 20, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                   step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_schedules
            WHERE user_id = @userId
              AND scheduled_date > @fromDate
              AND status IN ('Upcoming')
              AND is_deleted = FALSE
            ORDER BY scheduled_date ASC, step_number ASC
            LIMIT @limit;";

        var results = await connection.QueryAsync<RevisionSchedule>(new CommandDefinition(
            sql, new { userId, fromDate = fromDate.ToString("yyyy-MM-dd"), limit }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<RevisionSchedule>> GetSchedulesByTopicIdAsync(
        ulong userId, 
        ulong topicId, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                   step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_schedules
            WHERE user_id = @userId
              AND topic_id = @topicId
              AND is_deleted = FALSE
            ORDER BY step_number ASC;";

        var results = await connection.QueryAsync<RevisionSchedule>(new CommandDefinition(
            sql, new { userId, topicId }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<RevisionSchedule?> GetActiveScheduleForTopicAsync(
        ulong userId, 
        ulong topicId, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                SELECT id, user_id AS UserId, topic_id AS TopicId, revision_policy_id AS RevisionPolicyId,
                       step_number AS StepNumber, scheduled_date AS ScheduledDate, status,
                       completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                       created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
                FROM revision_schedules
                WHERE user_id = @userId
                  AND topic_id = @topicId
                  AND status IN ('Upcoming', 'DueToday', 'Overdue')
                  AND is_deleted = FALSE
                ORDER BY scheduled_date ASC
                LIMIT 1;";

            return await conn.QuerySingleOrDefaultAsync<RevisionSchedule>(new CommandDefinition(
                sql, new { userId, topicId }, transaction: transaction, cancellationToken: ct));
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<RevisionSchedule> CreateScheduleAsync(
        RevisionSchedule schedule, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                INSERT INTO revision_schedules
                    (user_id, topic_id, revision_policy_id, step_number, scheduled_date, status, completed_at_utc, is_deleted, created_at_utc, updated_at_utc)
                VALUES
                    (@UserId, @TopicId, @RevisionPolicyId, @StepNumber, @ScheduledDate, @Status, @CompletedAtUtc, @IsDeleted, UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await conn.ExecuteScalarAsync<ulong>(new CommandDefinition(
                sql, new {
                    schedule.UserId,
                    schedule.TopicId,
                    schedule.RevisionPolicyId,
                    schedule.StepNumber,
                    ScheduledDate = schedule.ScheduledDate.ToString("yyyy-MM-dd"),
                    schedule.Status,
                    schedule.CompletedAtUtc,
                    schedule.IsDeleted
                }, transaction: transaction, cancellationToken: ct));

            schedule.Id = id;
            return schedule;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<bool> UpdateScheduleAsync(
        RevisionSchedule schedule, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                UPDATE revision_schedules
                SET scheduled_date = @ScheduledDate,
                    status = @Status,
                    completed_at_utc = @CompletedAtUtc,
                    is_deleted = @IsDeleted,
                    updated_at_utc = UTC_TIMESTAMP()
                WHERE id = @Id AND user_id = @UserId;";

            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql, new {
                    schedule.Id,
                    schedule.UserId,
                    ScheduledDate = schedule.ScheduledDate.ToString("yyyy-MM-dd"),
                    schedule.Status,
                    schedule.CompletedAtUtc,
                    schedule.IsDeleted
                }, transaction: transaction, cancellationToken: ct));

            return rows > 0;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<int> BatchUpdateStatusesAsync(ulong userId, DateOnly today, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        // Update Upcoming -> DueToday for today's date
        const string updateDueTodaySql = @"
            UPDATE revision_schedules
            SET status = 'DueToday', updated_at_utc = UTC_TIMESTAMP()
            WHERE user_id = @userId
              AND scheduled_date = @today
              AND status = 'Upcoming'
              AND is_deleted = FALSE;";

        // Update DueToday/Upcoming -> Overdue for past dates
        const string updateOverdueSql = @"
            UPDATE revision_schedules
            SET status = 'Overdue', updated_at_utc = UTC_TIMESTAMP()
            WHERE user_id = @userId
              AND scheduled_date < @today
              AND status IN ('Upcoming', 'DueToday')
              AND is_deleted = FALSE;";

        var todayStr = today.ToString("yyyy-MM-dd");
        var rowsDueToday = await connection.ExecuteAsync(new CommandDefinition(updateDueTodaySql, new { userId, today = todayStr }, cancellationToken: ct));
        var rowsOverdue = await connection.ExecuteAsync(new CommandDefinition(updateOverdueSql, new { userId, today = todayStr }, cancellationToken: ct));

        return rowsDueToday + rowsOverdue;
    }

    public async Task<IReadOnlyList<(ulong UserId, ulong TopicId)>> GetCompletedTopicsMissingSchedulesAsync(ulong? userId = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT utp.user_id AS UserId, utp.topic_id AS TopicId
            FROM user_topic_progress utp
            WHERE utp.status IN ('Completed', 'Mastered')
              AND utp.is_custom_excluded = FALSE
              AND (@userId IS NULL OR utp.user_id = @userId)
              AND NOT EXISTS (
                  SELECT 1 FROM revision_schedules rs
                  WHERE rs.user_id = utp.user_id
                    AND rs.topic_id = utp.topic_id
                    AND rs.is_deleted = FALSE
              );";

        var rows = await connection.QueryAsync<ScheduleMissingRecord>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));

        return rows.Select(r => (r.UserId, r.TopicId)).ToList();
    }

    private class ScheduleMissingRecord
    {
        public ulong UserId { get; set; }
        public ulong TopicId { get; set; }
    }
}

