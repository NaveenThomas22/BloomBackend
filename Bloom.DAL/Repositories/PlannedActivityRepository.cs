using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class PlannedActivityRepository : IPlannedActivityRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PlannedActivityRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<PlannedActivity?> GetByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                   title, description, planned_start_at_utc AS PlannedStartAtUtc,
                   planned_end_at_utc AS PlannedEndAtUtc, timezone_id AS TimezoneId,
                   linked_topic_id AS LinkedTopicId, plan_status AS PlanStatus,
                   actual_activity_id AS ActualActivityId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM planned_activities
            WHERE id = @id AND user_id = @userId;";

        return await connection.QuerySingleOrDefaultAsync<PlannedActivity>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PlannedActivity>> GetPlannedByIntervalAsync(
        ulong userId, 
        DateTime startUtc, 
        DateTime endUtc, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, activity_category_id AS ActivityCategoryId,
                   title, description, planned_start_at_utc AS PlannedStartAtUtc,
                   planned_end_at_utc AS PlannedEndAtUtc, timezone_id AS TimezoneId,
                   linked_topic_id AS LinkedTopicId, plan_status AS PlanStatus,
                   actual_activity_id AS ActualActivityId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM planned_activities
            WHERE user_id = @userId
              AND planned_start_at_utc < @endUtc
              AND planned_end_at_utc > @startUtc
            ORDER BY planned_start_at_utc ASC;";

        var results = await connection.QueryAsync<PlannedActivity>(new CommandDefinition(
            sql, new { userId, startUtc, endUtc }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<PlannedActivity> CreatePlannedActivityAsync(PlannedActivity plan, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO planned_activities
                (user_id, activity_category_id, title, description, planned_start_at_utc, planned_end_at_utc, timezone_id, linked_topic_id, plan_status, actual_activity_id, created_at_utc, updated_at_utc)
            VALUES
                (@UserId, @ActivityCategoryId, @Title, @Description, @PlannedStartAtUtc, @PlannedEndAtUtc, @TimezoneId, @LinkedTopicId, @PlanStatus, @ActualActivityId, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, plan, cancellationToken: ct));
        plan.Id = id;
        return plan;
    }

    public async Task<bool> UpdatePlannedActivityAsync(PlannedActivity plan, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE planned_activities
            SET activity_category_id = @ActivityCategoryId,
                title = @Title,
                description = @Description,
                planned_start_at_utc = @PlannedStartAtUtc,
                planned_end_at_utc = @PlannedEndAtUtc,
                timezone_id = @TimezoneId,
                linked_topic_id = @LinkedTopicId,
                plan_status = @PlanStatus,
                actual_activity_id = @ActualActivityId,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, plan, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeletePlannedActivityAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM planned_activities WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }
}

