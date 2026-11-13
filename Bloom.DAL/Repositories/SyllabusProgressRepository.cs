using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class SyllabusProgressRepository : ISyllabusProgressRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SyllabusProgressRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<UserTopicProgress?> GetTopicProgressAsync(ulong userId, ulong topicId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, status, priority, 
                   target_date AS TargetDate, completion_date AS CompletionDate, 
                   confidence_rating AS ConfidenceRating, personal_notes AS PersonalNotes, 
                   is_custom_excluded AS IsCustomExcluded, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_topic_progress
            WHERE user_id = @userId AND topic_id = @topicId;";

        return await connection.QuerySingleOrDefaultAsync<UserTopicProgress>(new CommandDefinition(
            sql, 
            new { userId, topicId }, 
            cancellationToken: ct));
    }

    public async Task<IReadOnlyDictionary<ulong, UserTopicProgress>> GetTopicProgressMapAsync(ulong userId, IEnumerable<ulong> topicIds, CancellationToken ct = default)
    {
        var idList = topicIds.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<ulong, UserTopicProgress>();
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, status, priority, 
                   target_date AS TargetDate, completion_date AS CompletionDate, 
                   confidence_rating AS ConfidenceRating, personal_notes AS PersonalNotes, 
                   is_custom_excluded AS IsCustomExcluded, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_topic_progress
            WHERE user_id = @userId AND topic_id IN @idList;";

        var results = await connection.QueryAsync<UserTopicProgress>(new CommandDefinition(
            sql, 
            new { userId, idList }, 
            cancellationToken: ct));

        return results.ToDictionary(r => r.TopicId, r => r);
    }

    public async Task<UserTopicProgress> UpsertTopicProgressAsync(UserTopicProgress progress, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_topic_progress (
                user_id, topic_id, status, priority, target_date, completion_date, 
                confidence_rating, personal_notes, is_custom_excluded, created_at_utc, updated_at_utc
            )
            VALUES (
                @UserId, @TopicId, @Status, @Priority, @TargetDate, @CompletionDate, 
                @ConfidenceRating, @PersonalNotes, @IsCustomExcluded, UTC_TIMESTAMP(), UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                status = VALUES(status),
                priority = VALUES(priority),
                target_date = VALUES(target_date),
                completion_date = VALUES(completion_date),
                confidence_rating = VALUES(confidence_rating),
                personal_notes = VALUES(personal_notes),
                is_custom_excluded = VALUES(is_custom_excluded),
                updated_at_utc = UTC_TIMESTAMP();
            
            SELECT id, user_id AS UserId, topic_id AS TopicId, status, priority, 
                   target_date AS TargetDate, completion_date AS CompletionDate, 
                   confidence_rating AS ConfidenceRating, personal_notes AS PersonalNotes, 
                   is_custom_excluded AS IsCustomExcluded, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_topic_progress
            WHERE user_id = @UserId AND topic_id = @TopicId;";

        var result = await connection.QuerySingleAsync<UserTopicProgress>(new CommandDefinition(
            sql, 
            progress, 
            cancellationToken: ct));

        return result;
    }

    public async Task<UserSubtopicProgress?> GetSubtopicProgressAsync(ulong userId, ulong subtopicId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, subtopic_id AS SubtopicId, is_completed AS IsCompleted, 
                   completed_at_utc AS CompletedAtUtc, notes, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_subtopic_progress
            WHERE user_id = @userId AND subtopic_id = @subtopicId;";

        return await connection.QuerySingleOrDefaultAsync<UserSubtopicProgress>(new CommandDefinition(
            sql, 
            new { userId, subtopicId }, 
            cancellationToken: ct));
    }

    public async Task<IReadOnlyDictionary<ulong, UserSubtopicProgress>> GetSubtopicProgressMapAsync(ulong userId, IEnumerable<ulong> subtopicIds, CancellationToken ct = default)
    {
        var idList = subtopicIds.ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<ulong, UserSubtopicProgress>();
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, subtopic_id AS SubtopicId, is_completed AS IsCompleted, 
                   completed_at_utc AS CompletedAtUtc, notes, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_subtopic_progress
            WHERE user_id = @userId AND subtopic_id IN @idList;";

        var results = await connection.QueryAsync<UserSubtopicProgress>(new CommandDefinition(
            sql, 
            new { userId, idList }, 
            cancellationToken: ct));

        return results.ToDictionary(r => r.SubtopicId, r => r);
    }

    public async Task<UserSubtopicProgress> UpsertSubtopicProgressAsync(UserSubtopicProgress progress, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO user_subtopic_progress (
                user_id, subtopic_id, is_completed, completed_at_utc, notes, created_at_utc, updated_at_utc
            )
            VALUES (
                @UserId, @SubtopicId, @IsCompleted, @CompletedAtUtc, @Notes, UTC_TIMESTAMP(), UTC_TIMESTAMP()
            )
            ON DUPLICATE KEY UPDATE
                is_completed = VALUES(is_completed),
                completed_at_utc = VALUES(completed_at_utc),
                notes = VALUES(notes),
                updated_at_utc = UTC_TIMESTAMP();
            
            SELECT id, user_id AS UserId, subtopic_id AS SubtopicId, is_completed AS IsCompleted, 
                   completed_at_utc AS CompletedAtUtc, notes, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_subtopic_progress
            WHERE user_id = @UserId AND subtopic_id = @SubtopicId;";

        var result = await connection.QuerySingleAsync<UserSubtopicProgress>(new CommandDefinition(
            sql, 
            progress, 
            cancellationToken: ct));

        return result;
    }

    public async Task<IReadOnlyList<UserTopicProgress>> GetAllUserProgressAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, topic_id AS TopicId, status, priority, 
                   target_date AS TargetDate, completion_date AS CompletionDate, 
                   confidence_rating AS ConfidenceRating, personal_notes AS PersonalNotes, 
                   is_custom_excluded AS IsCustomExcluded, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM user_topic_progress
            WHERE user_id = @userId;";

        var results = await connection.QueryAsync<UserTopicProgress>(new CommandDefinition(
            sql, 
            new { userId }, 
            cancellationToken: ct));

        return results.AsList();
    }
}

