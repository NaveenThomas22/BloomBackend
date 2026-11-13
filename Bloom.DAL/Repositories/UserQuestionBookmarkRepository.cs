using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserQuestionBookmarkRepository : IUserQuestionBookmarkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserQuestionBookmarkRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<(IReadOnlyList<Question> Questions, int TotalCount)> GetBookmarksAsync(
        ulong userId, ulong? subjectId = null, ulong? topicId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var whereClauses = new List<string> { "b.user_id = @userId" };
        var parameters = new DynamicParameters();
        parameters.Add("userId", userId);

        if (subjectId.HasValue)
        {
            whereClauses.Add("q.subject_id = @subjectId");
            parameters.Add("subjectId", subjectId.Value);
        }
        if (topicId.HasValue)
        {
            whereClauses.Add("q.topic_id = @topicId");
            parameters.Add("topicId", topicId.Value);
        }

        var whereSql = string.Join(" AND ", whereClauses);

        var countSql = $@"
            SELECT COUNT(b.id)
            FROM user_question_bookmarks b
            JOIN questions q ON b.question_id = q.id
            WHERE {whereSql};";

        var totalCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(countSql, parameters, cancellationToken: ct));
        if (totalCount == 0) return (Array.Empty<Question>(), 0);

        var offset = (page - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var selectSql = $@"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.subtopic_id AS SubtopicId, q.question_set_id AS QuestionSetId, q.passage_id AS PassageId,
                   q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.question_image_url AS QuestionImageUrl,
                   q.estimated_seconds AS EstimatedSeconds, q.default_marks AS DefaultMarks,
                   q.default_penalty AS DefaultPenalty, q.tags, q.status,
                   s.name AS SubjectName, t.name AS TopicName
            FROM user_question_bookmarks b
            JOIN questions q ON b.question_id = q.id
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            WHERE {whereSql}
            ORDER BY b.created_at_utc DESC
            LIMIT @PageSize OFFSET @Offset;";

        var questions = (await connection.QueryAsync<Question>(new CommandDefinition(selectSql, parameters, cancellationToken: ct))).ToList();

        if (questions.Count > 0)
        {
            var qIds = questions.Select(q => q.Id).ToList();
            const string optSql = @"
                SELECT id, question_id AS QuestionId, option_key AS OptionKey, content, display_order AS DisplayOrder
                FROM question_options
                WHERE question_id IN @qIds
                ORDER BY display_order ASC;";

            var options = await connection.QueryAsync<QuestionOption>(new CommandDefinition(optSql, new { qIds }, cancellationToken: ct));
            var optMap = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var q in questions)
            {
                if (optMap.TryGetValue(q.Id, out var opts)) q.Options = opts;
            }
        }

        return (questions, totalCount);
    }

    public async Task<bool> IsBookmarkedAsync(ulong userId, ulong questionId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT COUNT(id) FROM user_question_bookmarks WHERE user_id = @userId AND question_id = @questionId;";
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { userId, questionId }, cancellationToken: ct));
        return count > 0;
    }

    public async Task<bool> AddBookmarkAsync(ulong userId, ulong questionId, string? note = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO user_question_bookmarks (user_id, question_id, note, created_at_utc)
            VALUES (@userId, @questionId, @note, UTC_TIMESTAMP())
            ON DUPLICATE KEY UPDATE note = VALUES(note);";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { userId, questionId, note }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> RemoveBookmarkAsync(ulong userId, ulong questionId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "DELETE FROM user_question_bookmarks WHERE user_id = @userId AND question_id = @questionId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { userId, questionId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<HashSet<ulong>> GetBookmarkedQuestionIdsAsync(ulong userId, IEnumerable<ulong> questionIds, CancellationToken ct = default)
    {
        var idList = questionIds.ToList();
        if (idList.Count == 0) return new HashSet<ulong>();

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT question_id FROM user_question_bookmarks WHERE user_id = @userId AND question_id IN @idList;";
        var ids = await connection.QueryAsync<ulong>(new CommandDefinition(sql, new { userId, idList }, cancellationToken: ct));
        return ids.ToHashSet();
    }
}

