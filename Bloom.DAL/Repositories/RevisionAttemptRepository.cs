using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class RevisionAttemptRepository : IRevisionAttemptRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RevisionAttemptRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<RevisionAttempt?> GetAttemptByIdAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, revision_schedule_id AS RevisionScheduleId, user_id AS UserId,
                   topic_id AS TopicId, revised_at_utc AS RevisedAtUtc, revision_date AS RevisionDate,
                   understanding_level AS UnderstandingLevel, confidence_rating AS ConfidenceRating,
                   notes, linked_activity_record_id AS LinkedActivityRecordId,
                   next_scheduled_date AS NextScheduledDate, created_at_utc AS CreatedAtUtc
            FROM revision_attempts
            WHERE id = @id AND user_id = @userId;";

        return await connection.QuerySingleOrDefaultAsync<RevisionAttempt>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<RevisionAttempt>> GetAttemptsByTopicIdAsync(ulong userId, ulong topicId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, revision_schedule_id AS RevisionScheduleId, user_id AS UserId,
                   topic_id AS TopicId, revised_at_utc AS RevisedAtUtc, revision_date AS RevisionDate,
                   understanding_level AS UnderstandingLevel, confidence_rating AS ConfidenceRating,
                   notes, linked_activity_record_id AS LinkedActivityRecordId,
                   next_scheduled_date AS NextScheduledDate, created_at_utc AS CreatedAtUtc
            FROM revision_attempts
            WHERE user_id = @userId AND topic_id = @topicId
            ORDER BY revised_at_utc DESC;";

        var results = await connection.QueryAsync<RevisionAttempt>(new CommandDefinition(
            sql, new { userId, topicId }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<IReadOnlyList<RevisionAttempt>> GetRecentAttemptsAsync(ulong userId, int limit = 50, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, revision_schedule_id AS RevisionScheduleId, user_id AS UserId,
                   topic_id AS TopicId, revised_at_utc AS RevisedAtUtc, revision_date AS RevisionDate,
                   understanding_level AS UnderstandingLevel, confidence_rating AS ConfidenceRating,
                   notes, linked_activity_record_id AS LinkedActivityRecordId,
                   next_scheduled_date AS NextScheduledDate, created_at_utc AS CreatedAtUtc
            FROM revision_attempts
            WHERE user_id = @userId
            ORDER BY revised_at_utc DESC
            LIMIT @limit;";

        var results = await connection.QueryAsync<RevisionAttempt>(new CommandDefinition(
            sql, new { userId, limit }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<RevisionAttempt> CreateAttemptAsync(
        RevisionAttempt attempt, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                INSERT INTO revision_attempts
                    (revision_schedule_id, user_id, topic_id, revised_at_utc, revision_date, understanding_level, confidence_rating, notes, linked_activity_record_id, next_scheduled_date, created_at_utc)
                VALUES
                    (@RevisionScheduleId, @UserId, @TopicId, @RevisedAtUtc, @RevisionDate, @UnderstandingLevel, @ConfidenceRating, @Notes, @LinkedActivityRecordId, @NextScheduledDate, UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await conn.ExecuteScalarAsync<ulong>(new CommandDefinition(
                sql, new {
                    attempt.RevisionScheduleId,
                    attempt.UserId,
                    attempt.TopicId,
                    attempt.RevisedAtUtc,
                    RevisionDate = attempt.RevisionDate.ToString("yyyy-MM-dd"),
                    attempt.UnderstandingLevel,
                    attempt.ConfidenceRating,
                    attempt.Notes,
                    attempt.LinkedActivityRecordId,
                    NextScheduledDate = attempt.NextScheduledDate?.ToString("yyyy-MM-dd")
                }, transaction: transaction, cancellationToken: ct));

            attempt.Id = id;
            return attempt;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<int> GetCompletedAttemptsCountAsync(
        ulong userId, 
        DateOnly startDate, 
        DateOnly endDate, 
        ulong? topicId = null, 
        ulong? subjectId = null, 
        CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT COUNT(ra.id)
            FROM revision_attempts ra
            LEFT JOIN syllabus_topics st ON ra.topic_id = st.id
            LEFT JOIN syllabus_modules sm ON st.module_id = sm.id
            WHERE ra.user_id = @userId
              AND ra.revision_date >= @startDate
              AND ra.revision_date <= @endDate
              AND (@topicId IS NULL OR ra.topic_id = @topicId)
              AND (@subjectId IS NULL OR sm.subject_id = @subjectId);";

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql, new {
                userId,
                startDate = startDate.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                topicId,
                subjectId
            }, cancellationToken: ct));
    }
}

