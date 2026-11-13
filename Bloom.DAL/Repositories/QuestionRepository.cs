using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public QuestionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<Question?> GetQuestionByIdAsync(ulong id, bool includeAnswerKey = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.subtopic_id AS SubtopicId, q.question_set_id AS QuestionSetId, q.passage_id AS PassageId,
                   q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.question_image_url AS QuestionImageUrl,
                   q.correct_answer AS CorrectAnswer, q.tita_numeric_range_min AS TitaNumericRangeMin,
                   q.tita_numeric_range_max AS TitaNumericRangeMax, q.explanation, q.solution_steps AS SolutionSteps,
                   q.estimated_seconds AS EstimatedSeconds, q.default_marks AS DefaultMarks,
                   q.default_penalty AS DefaultPenalty, q.source_attribution AS SourceAttribution,
                   q.tags, q.status, q.created_at_utc AS CreatedAtUtc, q.updated_at_utc AS UpdatedAtUtc,
                   s.name AS SubjectName, t.name AS TopicName
            FROM questions q
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            WHERE q.id = @id;";

        var question = await connection.QuerySingleOrDefaultAsync<Question>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));

        if (question == null) return null;

        // Fetch options
        const string optSql = @"
            SELECT id, question_id AS QuestionId, option_key AS OptionKey, content,
                   is_correct AS IsCorrect, display_order AS DisplayOrder, created_at_utc AS CreatedAtUtc
            FROM question_options
            WHERE question_id = @id
            ORDER BY display_order ASC;";

        var options = (await connection.QueryAsync<QuestionOption>(
            new CommandDefinition(optSql, new { id }, cancellationToken: ct))).ToList();

        if (!includeAnswerKey)
        {
            // Protect answer key, tolerance ranges, and explanations from candidate view
            question.CorrectAnswer = string.Empty;
            question.TitaNumericRangeMin = null;
            question.TitaNumericRangeMax = null;
            question.Explanation = null;
            question.SolutionSteps = null;
            foreach (var opt in options)
            {
                opt.IsCorrect = false;
            }
        }

        question.Options = options;

        if (question.PassageId.HasValue)
        {
            const string pSql = "SELECT * FROM question_passages WHERE id = @passageId;";
            question.Passage = await connection.QuerySingleOrDefaultAsync<QuestionPassage>(
                new CommandDefinition(pSql, new { passageId = question.PassageId.Value }, cancellationToken: ct));
        }

        if (question.QuestionSetId.HasValue)
        {
            const string sSql = "SELECT * FROM question_sets WHERE id = @setId;";
            question.QuestionSet = await connection.QuerySingleOrDefaultAsync<QuestionSet>(
                new CommandDefinition(sSql, new { setId = question.QuestionSetId.Value }, cancellationToken: ct));
        }

        return question;
    }

    public async Task<(IReadOnlyList<Question> Questions, int TotalCount)> GetQuestionsPagedAsync(
        QuestionFilter filter, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.SubjectId.HasValue)
        {
            whereClauses.Add("q.subject_id = @SubjectId");
            parameters.Add("SubjectId", filter.SubjectId.Value);
        }
        if (filter.ModuleId.HasValue)
        {
            whereClauses.Add("q.module_id = @ModuleId");
            parameters.Add("ModuleId", filter.ModuleId.Value);
        }
        if (filter.TopicId.HasValue)
        {
            whereClauses.Add("q.topic_id = @TopicId");
            parameters.Add("TopicId", filter.TopicId.Value);
        }
        if (filter.SubtopicId.HasValue)
        {
            whereClauses.Add("q.subtopic_id = @SubtopicId");
            parameters.Add("SubtopicId", filter.SubtopicId.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.QuestionType))
        {
            whereClauses.Add("q.question_type = @QuestionType");
            parameters.Add("QuestionType", filter.QuestionType);
        }
        if (!string.IsNullOrWhiteSpace(filter.DifficultyLevel))
        {
            whereClauses.Add("q.difficulty_level = @DifficultyLevel");
            parameters.Add("DifficultyLevel", filter.DifficultyLevel);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            whereClauses.Add("q.status = @Status");
            parameters.Add("Status", filter.Status);
        }
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            whereClauses.Add("(q.question_content LIKE @Search OR q.tags LIKE @Search)");
            parameters.Add("Search", $"%{filter.SearchTerm.Trim()}%");
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        var countSql = $"SELECT COUNT(q.id) FROM questions q {whereSql};";
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct));

        if (totalCount == 0)
        {
            return (Array.Empty<Question>(), 0);
        }

        var offset = (page - 1) * pageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", pageSize);

        var selectSql = $@"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.subtopic_id AS SubtopicId, q.question_set_id AS QuestionSetId, q.passage_id AS PassageId,
                   q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.question_image_url AS QuestionImageUrl,
                   q.estimated_seconds AS EstimatedSeconds, q.default_marks AS DefaultMarks,
                   q.default_penalty AS DefaultPenalty, q.source_attribution AS SourceAttribution,
                   q.tags, q.status, q.created_at_utc AS CreatedAtUtc, q.updated_at_utc AS UpdatedAtUtc,
                   s.name AS SubjectName, t.name AS TopicName
            FROM questions q
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            {whereSql}
            ORDER BY q.id ASC
            LIMIT @PageSize OFFSET @Offset;";

        var questions = (await connection.QueryAsync<Question>(
            new CommandDefinition(selectSql, parameters, cancellationToken: ct))).ToList();

        if (questions.Count > 0)
        {
            var qIds = questions.Select(q => q.Id).ToList();
            const string optSql = @"
                SELECT id, question_id AS QuestionId, option_key AS OptionKey, content,
                       display_order AS DisplayOrder
                FROM question_options
                WHERE question_id IN @qIds
                ORDER BY display_order ASC;";

            var options = await connection.QueryAsync<QuestionOption>(
                new CommandDefinition(optSql, new { qIds }, cancellationToken: ct));

            var optMap = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var q in questions)
            {
                if (optMap.TryGetValue(q.Id, out var opts))
                {
                    q.Options = opts;
                }
            }
        }

        return (questions, totalCount);
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsForPracticeAsync(
        ulong? subjectId, ulong? topicId, string? difficulty, string? questionType, int count, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var whereClauses = new List<string> { "q.status = 'Published'" };
        var parameters = new DynamicParameters();

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
        if (!string.IsNullOrWhiteSpace(difficulty) && !string.Equals(difficulty, "Mixed", StringComparison.OrdinalIgnoreCase))
        {
            whereClauses.Add("q.difficulty_level = @difficulty");
            parameters.Add("difficulty", difficulty);
        }
        if (!string.IsNullOrWhiteSpace(questionType))
        {
            whereClauses.Add("q.question_type = @questionType");
            parameters.Add("questionType", questionType);
        }

        parameters.Add("count", count);

        var whereSql = string.Join(" AND ", whereClauses);
        var sql = $@"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.subtopic_id AS SubtopicId, q.question_set_id AS QuestionSetId, q.passage_id AS PassageId,
                   q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.question_image_url AS QuestionImageUrl,
                   q.estimated_seconds AS EstimatedSeconds, q.default_marks AS DefaultMarks,
                   q.default_penalty AS DefaultPenalty, q.tags, q.status,
                   s.name AS SubjectName, t.name AS TopicName
            FROM questions q
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            WHERE {whereSql}
            ORDER BY RAND()
            LIMIT @count;";

        var questions = (await connection.QueryAsync<Question>(
            new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();

        await PopulateOptionsAndContextAsync(connection, questions, ct);
        return questions;
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsByPassageIdAsync(ulong passageId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.passage_id AS PassageId, q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.estimated_seconds AS EstimatedSeconds,
                   q.default_marks AS DefaultMarks, q.default_penalty AS DefaultPenalty,
                   s.name AS SubjectName, t.name AS TopicName
            FROM questions q
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            WHERE q.passage_id = @passageId AND q.status = 'Published'
            ORDER BY q.id ASC;";

        var questions = (await connection.QueryAsync<Question>(
            new CommandDefinition(sql, new { passageId }, cancellationToken: ct))).ToList();

        await PopulateOptionsAndContextAsync(connection, questions, ct);
        return questions;
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsBySetIdAsync(ulong setId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT q.id, q.subject_id AS SubjectId, q.module_id AS ModuleId, q.topic_id AS TopicId,
                   q.question_set_id AS QuestionSetId, q.question_type AS QuestionType, q.difficulty_level AS DifficultyLevel,
                   q.question_content AS QuestionContent, q.estimated_seconds AS EstimatedSeconds,
                   q.default_marks AS DefaultMarks, q.default_penalty AS DefaultPenalty,
                   s.name AS SubjectName, t.name AS TopicName
            FROM questions q
            JOIN syllabus_subjects s ON q.subject_id = s.id
            JOIN syllabus_topics t ON q.topic_id = t.id
            WHERE q.question_set_id = @setId AND q.status = 'Published'
            ORDER BY q.id ASC;";

        var questions = (await connection.QueryAsync<Question>(
            new CommandDefinition(sql, new { setId }, cancellationToken: ct))).ToList();

        await PopulateOptionsAndContextAsync(connection, questions, ct);
        return questions;
    }

    public async Task<Question> CreateQuestionAsync(Question question, List<QuestionOption> options, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO questions
                    (subject_id, module_id, topic_id, subtopic_id, question_set_id, passage_id,
                     question_type, difficulty_level, question_content, question_image_url,
                     correct_answer, tita_numeric_range_min, tita_numeric_range_max, explanation,
                     solution_steps, estimated_seconds, default_marks, default_penalty,
                     source_attribution, tags, status, created_at_utc, updated_at_utc)
                VALUES
                    (@SubjectId, @ModuleId, @TopicId, @SubtopicId, @QuestionSetId, @PassageId,
                     (@QuestionType), (@DifficultyLevel), @QuestionContent, @QuestionImageUrl,
                     @CorrectAnswer, @TitaNumericRangeMin, @TitaNumericRangeMax, @Explanation,
                     @SolutionSteps, @EstimatedSeconds, @DefaultMarks, @DefaultPenalty,
                     @SourceAttribution, @Tags, (@Status), UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await connection.ExecuteScalarAsync<ulong>(
                new CommandDefinition(sql, question, transaction: tx, cancellationToken: ct));

            question.Id = id;

            if (options.Count > 0)
            {
                const string optSql = @"
                    INSERT INTO question_options
                        (question_id, option_key, content, is_correct, display_order, created_at_utc)
                    VALUES
                        (@QuestionId, @OptionKey, @Content, @IsCorrect, @DisplayOrder, UTC_TIMESTAMP());";

                foreach (var opt in options)
                {
                    opt.QuestionId = id;
                    await connection.ExecuteAsync(new CommandDefinition(optSql, opt, transaction: tx, cancellationToken: ct));
                }
            }

            tx.Commit();
            question.Options = options;
            return question;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateQuestionAsync(Question question, List<QuestionOption>? options = null, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction();

        try
        {
            const string sql = @"
                UPDATE questions
                SET subject_id = @SubjectId,
                    module_id = @ModuleId,
                    topic_id = @TopicId,
                    subtopic_id = @SubtopicId,
                    question_set_id = @QuestionSetId,
                    passage_id = @PassageId,
                    question_type = @QuestionType,
                    difficulty_level = @DifficultyLevel,
                    question_content = @QuestionContent,
                    question_image_url = @QuestionImageUrl,
                    correct_answer = @CorrectAnswer,
                    tita_numeric_range_min = @TitaNumericRangeMin,
                    tita_numeric_range_max = @TitaNumericRangeMax,
                    explanation = @Explanation,
                    solution_steps = @SolutionSteps,
                    estimated_seconds = @EstimatedSeconds,
                    default_marks = @DefaultMarks,
                    default_penalty = @DefaultPenalty,
                    source_attribution = @SourceAttribution,
                    tags = @Tags,
                    status = @Status,
                    updated_at_utc = UTC_TIMESTAMP()
                WHERE id = @Id;";

            var rows = await connection.ExecuteAsync(new CommandDefinition(sql, question, transaction: tx, cancellationToken: ct));

            if (options != null)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "DELETE FROM question_options WHERE question_id = @id;", new { id = question.Id }, transaction: tx, cancellationToken: ct));

                const string optSql = @"
                    INSERT INTO question_options
                        (question_id, option_key, content, is_correct, display_order, created_at_utc)
                    VALUES
                        (@QuestionId, @OptionKey, @Content, @IsCorrect, @DisplayOrder, UTC_TIMESTAMP());";

                foreach (var opt in options)
                {
                    opt.QuestionId = question.Id;
                    await connection.ExecuteAsync(new CommandDefinition(optSql, opt, transaction: tx, cancellationToken: ct));
                }
            }

            tx.Commit();
            return rows > 0;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateQuestionStatusAsync(ulong questionId, string status, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "UPDATE questions SET status = @status, updated_at_utc = UTC_TIMESTAMP() WHERE id = @questionId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { questionId, status }, cancellationToken: ct));
        return rows > 0;
    }

    private static async Task PopulateOptionsAndContextAsync(IDbConnection connection, List<Question> questions, CancellationToken ct)
    {
        if (questions.Count == 0) return;

        var qIds = questions.Select(q => q.Id).ToList();
        const string optSql = @"
            SELECT id, question_id AS QuestionId, option_key AS OptionKey, content, display_order AS DisplayOrder
            FROM question_options
            WHERE question_id IN @qIds
            ORDER BY display_order ASC;";

        var options = await connection.QueryAsync<QuestionOption>(
            new CommandDefinition(optSql, new { qIds }, cancellationToken: ct));

        var optMap = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

        var passageIds = questions.Where(q => q.PassageId.HasValue).Select(q => q.PassageId!.Value).Distinct().ToList();
        Dictionary<ulong, QuestionPassage> passageMap = new();
        if (passageIds.Count > 0)
        {
            const string pSql = "SELECT * FROM question_passages WHERE id IN @passageIds;";
            var passages = await connection.QueryAsync<QuestionPassage>(new CommandDefinition(pSql, new { passageIds }, cancellationToken: ct));
            passageMap = passages.ToDictionary(p => p.Id);
        }

        var setIds = questions.Where(q => q.QuestionSetId.HasValue).Select(q => q.QuestionSetId!.Value).Distinct().ToList();
        Dictionary<ulong, QuestionSet> setMap = new();
        if (setIds.Count > 0)
        {
            const string sSql = "SELECT * FROM question_sets WHERE id IN @setIds;";
            var sets = await connection.QueryAsync<QuestionSet>(new CommandDefinition(sSql, new { setIds }, cancellationToken: ct));
            setMap = sets.ToDictionary(s => s.Id);
        }

        foreach (var q in questions)
        {
            if (optMap.TryGetValue(q.Id, out var opts)) q.Options = opts;
            if (q.PassageId.HasValue && passageMap.TryGetValue(q.PassageId.Value, out var pass)) q.Passage = pass;
            if (q.QuestionSetId.HasValue && setMap.TryGetValue(q.QuestionSetId.Value, out var qset)) q.QuestionSet = qset;
        }
    }
}

