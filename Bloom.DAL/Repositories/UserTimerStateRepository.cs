using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserTimerStateRepository : IUserTimerStateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserTimerStateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<UserTimerState?> GetByUserIdAsync(
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
                SELECT user_id AS UserId, session_group_id AS SessionGroupId, 
                       activity_category_id AS ActivityCategoryId, topic_id AS TopicId,
                       title, study_mode AS StudyMode, timer_state AS TimerState,
                       current_interval_id AS CurrentIntervalId, started_at_utc AS StartedAtUtc,
                       last_paused_at_utc AS LastPausedAtUtc, created_at_utc AS CreatedAtUtc,
                       updated_at_utc AS UpdatedAtUtc
                FROM user_timer_states
                WHERE user_id = @userId;";

            return await conn.QuerySingleOrDefaultAsync<UserTimerState>(new CommandDefinition(
                sql, new { userId }, transaction: transaction, cancellationToken: ct));
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<UserTimerState> UpsertAsync(
        UserTimerState state, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                INSERT INTO user_timer_states
                    (user_id, session_group_id, activity_category_id, topic_id, title, study_mode, timer_state, current_interval_id, started_at_utc, last_paused_at_utc, created_at_utc, updated_at_utc)
                VALUES
                    (@UserId, @SessionGroupId, @ActivityCategoryId, @TopicId, @Title, @StudyMode, @TimerState, @CurrentIntervalId, @StartedAtUtc, @LastPausedAtUtc, UTC_TIMESTAMP(), UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    session_group_id = VALUES(session_group_id),
                    activity_category_id = VALUES(activity_category_id),
                    topic_id = VALUES(topic_id),
                    title = VALUES(title),
                    study_mode = VALUES(study_mode),
                    timer_state = VALUES(timer_state),
                    current_interval_id = VALUES(current_interval_id),
                    started_at_utc = VALUES(started_at_utc),
                    last_paused_at_utc = VALUES(last_paused_at_utc),
                    updated_at_utc = UTC_TIMESTAMP();";

            await conn.ExecuteAsync(new CommandDefinition(sql, state, transaction: transaction, cancellationToken: ct));
            return state;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<bool> DeleteAsync(
        ulong userId, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = "DELETE FROM user_timer_states WHERE user_id = @userId;";
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql, new { userId }, transaction: transaction, cancellationToken: ct));
            return rows > 0;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }
}
