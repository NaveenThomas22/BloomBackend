using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class PersonalMilestoneRepository : IPersonalMilestoneRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PersonalMilestoneRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<PersonalMilestone>> GetMilestonesAsync(ulong userId, string? status = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var sql = @"
            SELECT id, user_id AS UserId, title, description, category,
                   target_value AS TargetValue, current_value AS CurrentValue, unit,
                   target_date AS TargetDate, status, achieved_at_utc AS AchievedAtUtc,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
            FROM personal_milestones
            WHERE user_id = @userId AND deleted_at_utc IS NULL " +
            (!string.IsNullOrWhiteSpace(status) ? "AND status = @status " : "") +
            "ORDER BY CASE status WHEN 'InProgress' THEN 1 WHEN 'Achieved' THEN 2 ELSE 3 END, created_at_utc DESC;";

        var result = await connection.QueryAsync<PersonalMilestone>(
            new CommandDefinition(sql, new { userId, status }, cancellationToken: ct));

        return result.ToList().AsReadOnly();
    }

    public async Task<PersonalMilestone?> GetMilestoneByIdAsync(ulong milestoneId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, category,
                   target_value AS TargetValue, current_value AS CurrentValue, unit,
                   target_date AS TargetDate, status, achieved_at_utc AS AchievedAtUtc,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc, deleted_at_utc AS DeletedAtUtc
            FROM personal_milestones
            WHERE id = @milestoneId AND user_id = @userId AND deleted_at_utc IS NULL;";

        return await connection.QuerySingleOrDefaultAsync<PersonalMilestone>(
            new CommandDefinition(sql, new { milestoneId, userId }, cancellationToken: ct));
    }

    public async Task<ulong> CreateMilestoneAsync(PersonalMilestone milestone, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO personal_milestones (user_id, title, description, category, target_value, current_value, unit,
                                             target_date, status, achieved_at_utc, created_at_utc, updated_at_utc)
            VALUES (@UserId, @Title, @Description, @Category, @TargetValue, @CurrentValue, @Unit,
                    @TargetDate, @Status, @AchievedAtUtc, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, new
            {
                milestone.UserId,
                milestone.Title,
                milestone.Description,
                milestone.Category,
                milestone.TargetValue,
                milestone.CurrentValue,
                milestone.Unit,
                TargetDate = milestone.TargetDate.HasValue ? milestone.TargetDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                milestone.Status,
                milestone.AchievedAtUtc
            }, cancellationToken: ct));
    }

    public async Task<bool> UpdateMilestoneAsync(PersonalMilestone milestone, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE personal_milestones
            SET title = @Title,
                description = @Description,
                category = @Category,
                target_value = @TargetValue,
                current_value = @CurrentValue,
                unit = @Unit,
                target_date = @TargetDate,
                status = @Status,
                achieved_at_utc = @AchievedAtUtc,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                milestone.Id,
                milestone.UserId,
                milestone.Title,
                milestone.Description,
                milestone.Category,
                milestone.TargetValue,
                milestone.CurrentValue,
                milestone.Unit,
                TargetDate = milestone.TargetDate.HasValue ? milestone.TargetDate.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                milestone.Status,
                milestone.AchievedAtUtc
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> DeleteMilestoneAsync(ulong milestoneId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE personal_milestones
            SET deleted_at_utc = UTC_TIMESTAMP()
            WHERE id = @milestoneId AND user_id = @userId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { milestoneId, userId }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> UpdateProgressAsync(ulong milestoneId, ulong userId, decimal currentValue, string status, DateTime? achievedAtUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE personal_milestones
            SET current_value = @currentValue,
                status = @status,
                achieved_at_utc = @achievedAtUtc,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @milestoneId AND user_id = @userId AND deleted_at_utc IS NULL;";

        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { milestoneId, userId, currentValue, status, achievedAtUtc }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<MilestoneSystemMetrics> GetUserSystemMetricsAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 
                (SELECT COALESCE(SUM(TIMESTAMPDIFF(SECOND, ar.start_at_utc, ar.end_at_utc)), 0) / 3600.0 
                 FROM activity_records ar 
                 INNER JOIN study_session_details ssd ON ssd.activity_id = ar.id 
                 WHERE ar.user_id = @userId 
                   AND ar.activity_status = 'Completed' 
                   AND ar.is_planned = 0 
                   AND ar.end_at_utc IS NOT NULL 
                   AND ar.deleted_at_utc IS NULL) AS StudyHours,
                (SELECT COUNT(*) 
                 FROM question_attempts 
                 WHERE user_id = @userId) AS QuestionsSolved,
                (SELECT COUNT(*) 
                 FROM user_topic_progress 
                 WHERE user_id = @userId 
                   AND status IN ('Mastered', 'Completed')) AS TopicsMastered,
                (SELECT COUNT(*) 
                 FROM exam_attempts 
                 WHERE user_id = @userId 
                   AND status IN ('Submitted', 'AutoSubmitted')) AS MockTestsCompleted;";

        var result = await connection.QuerySingleOrDefaultAsync<MilestoneSystemMetrics>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));

        return result ?? new MilestoneSystemMetrics();
    }
}
