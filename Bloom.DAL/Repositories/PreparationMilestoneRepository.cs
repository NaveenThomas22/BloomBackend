using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class PreparationMilestoneRepository : IPreparationMilestoneRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PreparationMilestoneRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<PreparationMilestone?> GetMilestoneByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, target_date AS TargetDate,
                   subject_id AS SubjectId, topic_id AS TopicId, priority, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM preparation_milestones
            WHERE id = @id AND user_id = @userId AND is_deleted = FALSE;";

        return await connection.QuerySingleOrDefaultAsync<PreparationMilestone>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<PreparationMilestone>> GetMilestonesAsync(
        ulong userId, 
        string? status = null, 
        bool includeDeleted = false, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, title, description, target_date AS TargetDate,
                   subject_id AS SubjectId, topic_id AS TopicId, priority, status,
                   completed_at_utc AS CompletedAtUtc, is_deleted AS IsDeleted,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM preparation_milestones
            WHERE user_id = @userId
              AND (@status IS NULL OR status = @status)
              AND (@includeDeleted = TRUE OR is_deleted = FALSE)
            ORDER BY target_date ASC, id DESC;";

        var results = await connection.QueryAsync<PreparationMilestone>(new CommandDefinition(
            sql, new { userId, status, includeDeleted }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<PreparationMilestone> CreateMilestoneAsync(PreparationMilestone milestone, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO preparation_milestones
                (user_id, title, description, target_date, subject_id, topic_id, priority, status, completed_at_utc, is_deleted, created_at_utc, updated_at_utc)
            VALUES
                (@UserId, @Title, @Description, @TargetDate, @SubjectId, @TopicId, @Priority, @Status, @CompletedAtUtc, @IsDeleted, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(
            sql, new {
                milestone.UserId,
                milestone.Title,
                milestone.Description,
                TargetDate = milestone.TargetDate.ToString("yyyy-MM-dd"),
                milestone.SubjectId,
                milestone.TopicId,
                milestone.Priority,
                milestone.Status,
                milestone.CompletedAtUtc,
                milestone.IsDeleted
            }, cancellationToken: ct));

        milestone.Id = id;
        return milestone;
    }

    public async Task<bool> UpdateMilestoneAsync(PreparationMilestone milestone, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE preparation_milestones
            SET title = @Title,
                description = @Description,
                target_date = @TargetDate,
                subject_id = @SubjectId,
                topic_id = @TopicId,
                priority = @Priority,
                status = @Status,
                completed_at_utc = @CompletedAtUtc,
                is_deleted = @IsDeleted,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new {
                milestone.Id,
                milestone.UserId,
                milestone.Title,
                milestone.Description,
                TargetDate = milestone.TargetDate.ToString("yyyy-MM-dd"),
                milestone.SubjectId,
                milestone.TopicId,
                milestone.Priority,
                milestone.Status,
                milestone.CompletedAtUtc,
                milestone.IsDeleted
            }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> SoftDeleteMilestoneAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE preparation_milestones
            SET is_deleted = TRUE,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @id AND user_id = @userId AND is_deleted = FALSE;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }
}

