using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserGoalRepository : IUserGoalRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserGoalRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<UserGoal?> GetGoalByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, goal_period AS GoalPeriod,
                   target_metric AS TargetMetric, target_value AS TargetValue, unit,
                   start_date AS StartDate, end_date AS EndDate, subject_id AS SubjectId,
                   topic_id AS TopicId, status, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_goals
            WHERE id = @id AND user_id = @userId AND is_deleted = FALSE;";

        return await connection.QuerySingleOrDefaultAsync<UserGoal>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<UserGoal>> GetGoalsAsync(
        ulong userId, 
        string? period = null, 
        string? status = null, 
        bool includeDeleted = false, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, goal_period AS GoalPeriod,
                   target_metric AS TargetMetric, target_value AS TargetValue, unit,
                   start_date AS StartDate, end_date AS EndDate, subject_id AS SubjectId,
                   topic_id AS TopicId, status, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_goals
            WHERE user_id = @userId
              AND (@period IS NULL OR goal_period = @period)
              AND (@status IS NULL OR status = @status)
              AND (@includeDeleted = TRUE OR is_deleted = FALSE)
            ORDER BY end_date ASC, id DESC;";

        var results = await connection.QueryAsync<UserGoal>(new CommandDefinition(
            sql, new { userId, period, status, includeDeleted }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<UserGoal>> GetActiveGoalsForDateAsync(
        ulong userId, 
        DateOnly date, 
        string? period = null, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, goal_period AS GoalPeriod,
                   target_metric AS TargetMetric, target_value AS TargetValue, unit,
                   start_date AS StartDate, end_date AS EndDate, subject_id AS SubjectId,
                   topic_id AS TopicId, status, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_goals
            WHERE user_id = @userId
              AND start_date <= @date
              AND end_date >= @date
              AND status = 'Active'
              AND (@period IS NULL OR goal_period = @period)
              AND is_deleted = FALSE
            ORDER BY goal_period ASC, end_date ASC;";

        var results = await connection.QueryAsync<UserGoal>(new CommandDefinition(
            sql, new { userId, date = date.ToString("yyyy-MM-dd"), period }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<UserGoal>> GetActiveGoalsForRangeAsync(
        ulong userId, 
        DateOnly startDate, 
        DateOnly endDate, 
        string? period = null, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, goal_period AS GoalPeriod,
                   target_metric AS TargetMetric, target_value AS TargetValue, unit,
                   start_date AS StartDate, end_date AS EndDate, subject_id AS SubjectId,
                   topic_id AS TopicId, status, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_goals
            WHERE user_id = @userId
              AND start_date <= @endDate
              AND end_date >= @startDate
              AND status = 'Active'
              AND (@period IS NULL OR goal_period = @period)
              AND is_deleted = FALSE
            ORDER BY start_date ASC, end_date ASC;";

        var results = await connection.QueryAsync<UserGoal>(new CommandDefinition(
            sql, new { userId, startDate = startDate.ToString("yyyy-MM-dd"), endDate = endDate.ToString("yyyy-MM-dd"), period }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<UserGoal> CreateGoalAsync(UserGoal goal, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_goals
                (user_id, title, description, goal_period, target_metric, target_value, unit, start_date, end_date, subject_id, topic_id, status, is_deleted, created_at_utc, updated_at_utc)
            VALUES
                (@UserId, @Title, @Description, @GoalPeriod, @TargetMetric, @TargetValue, @Unit, @StartDate, @EndDate, @SubjectId, @TopicId, @Status, @IsDeleted, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(
            sql, new {
                goal.UserId,
                goal.Title,
                goal.Description,
                goal.GoalPeriod,
                goal.TargetMetric,
                goal.TargetValue,
                goal.Unit,
                StartDate = goal.StartDate.ToString("yyyy-MM-dd"),
                EndDate = goal.EndDate.ToString("yyyy-MM-dd"),
                goal.SubjectId,
                goal.TopicId,
                goal.Status,
                goal.IsDeleted
            }, cancellationToken: ct));

        goal.Id = id;
        return goal;
    }

    public async Task<bool> UpdateGoalAsync(UserGoal goal, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_goals
            SET title = @Title,
                description = @Description,
                goal_period = @GoalPeriod,
                target_metric = @TargetMetric,
                target_value = @TargetValue,
                unit = @Unit,
                start_date = @StartDate,
                end_date = @EndDate,
                subject_id = @SubjectId,
                topic_id = @TopicId,
                status = @Status,
                is_deleted = @IsDeleted,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new {
                goal.Id,
                goal.UserId,
                goal.Title,
                goal.Description,
                goal.GoalPeriod,
                goal.TargetMetric,
                goal.TargetValue,
                goal.Unit,
                StartDate = goal.StartDate.ToString("yyyy-MM-dd"),
                EndDate = goal.EndDate.ToString("yyyy-MM-dd"),
                goal.SubjectId,
                goal.TopicId,
                goal.Status,
                goal.IsDeleted
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> SoftDeleteGoalAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE user_goals
            SET is_deleted = TRUE,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @id AND user_id = @userId AND is_deleted = FALSE;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }
}

