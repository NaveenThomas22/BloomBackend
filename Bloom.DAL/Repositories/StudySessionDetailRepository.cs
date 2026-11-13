using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class StudySessionDetailRepository : IStudySessionDetailRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public StudySessionDetailRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<StudySessionDetail?> GetByActivityIdAsync(ulong activityId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT activity_id AS ActivityId, topic_id AS TopicId, study_mode AS StudyMode,
                   difficulty_level AS DifficultyLevel, questions_attempted AS QuestionsAttempted,
                   questions_correct AS QuestionsCorrect, session_notes AS SessionNotes,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM study_session_details
            WHERE activity_id = @activityId;";

        return await connection.QuerySingleOrDefaultAsync<StudySessionDetail>(new CommandDefinition(
            sql, new { activityId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyDictionary<ulong, StudySessionDetail>> GetByActivityIdsAsync(
        IEnumerable<ulong> activityIds, 
        CancellationToken ct = default)
    {
        var ids = activityIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<ulong, StudySessionDetail>();
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT activity_id AS ActivityId, topic_id AS TopicId, study_mode AS StudyMode,
                   difficulty_level AS DifficultyLevel, questions_attempted AS QuestionsAttempted,
                   questions_correct AS QuestionsCorrect, session_notes AS SessionNotes,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM study_session_details
            WHERE activity_id IN @ids;";

        var results = await connection.QueryAsync<StudySessionDetail>(new CommandDefinition(
            sql, new { ids }, cancellationToken: ct));

        return results.ToDictionary(r => r.ActivityId, r => r);
    }

    public async Task<StudySessionDetail> UpsertAsync(
        StudySessionDetail detail, 
        IDbConnection? connection = null, 
        IDbTransaction? transaction = null, 
        CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);
        try
        {
            const string sql = @"
                INSERT INTO study_session_details 
                    (activity_id, topic_id, study_mode, difficulty_level, questions_attempted, questions_correct, session_notes, created_at_utc, updated_at_utc)
                VALUES 
                    (@ActivityId, @TopicId, @StudyMode, @DifficultyLevel, @QuestionsAttempted, @QuestionsCorrect, @SessionNotes, UTC_TIMESTAMP(), UTC_TIMESTAMP())
                ON DUPLICATE KEY UPDATE
                    topic_id = VALUES(topic_id),
                    study_mode = VALUES(study_mode),
                    difficulty_level = VALUES(difficulty_level),
                    questions_attempted = VALUES(questions_attempted),
                    questions_correct = VALUES(questions_correct),
                    session_notes = VALUES(session_notes),
                    updated_at_utc = UTC_TIMESTAMP();";

            await conn.ExecuteAsync(new CommandDefinition(sql, detail, transaction: transaction, cancellationToken: ct));
            return detail;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }
}
