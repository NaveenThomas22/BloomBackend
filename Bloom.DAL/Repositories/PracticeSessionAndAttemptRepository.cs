using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class PracticeSessionRepository : IPracticeSessionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PracticeSessionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<PracticeSession> CreateSessionAsync(
        PracticeSession session, List<PracticeSessionQuestion> sessionQuestions, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO practice_sessions
                    (user_id, title, practice_mode, subject_id, topic_id, difficulty_level,
                     total_questions, attempted_questions, correct_answers, incorrect_answers,
                     unanswered_questions, score, accuracy_percentage, time_spent_seconds,
                     time_limit_seconds, status, started_at_utc, completed_at_utc, activity_record_id)
                VALUES
                    (@UserId, @Title, @PracticeMode, @SubjectId, @TopicId, @DifficultyLevel,
                     @TotalQuestions, @AttemptedQuestions, @CorrectAnswers, @IncorrectAnswers,
                     @UnansweredQuestions, @Score, @AccuracyPercentage, @TimeSpentSeconds,
                     @TimeLimitSeconds, @Status, @StartedAtUtc, @CompletedAtUtc, @ActivityRecordId);
                SELECT LAST_INSERT_ID();";

            var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, session, transaction: tx, cancellationToken: ct));
            session.Id = id;

            const string qSql = @"
                INSERT INTO practice_session_questions
                    (practice_session_id, question_id, display_order, user_answer, is_answered,
                     is_correct, is_marked_for_review, time_spent_seconds, answered_at_utc)
                VALUES
                    (@PracticeSessionId, @QuestionId, @DisplayOrder, @UserAnswer, @IsAnswered,
                     @IsCorrect, @IsMarkedForReview, @TimeSpentSeconds, @AnsweredAtUtc);";

            foreach (var sq in sessionQuestions)
            {
                sq.PracticeSessionId = id;
                await connection.ExecuteAsync(new CommandDefinition(qSql, sq, transaction: tx, cancellationToken: ct));
            }

            tx.Commit();
            session.Questions = sessionQuestions;
            return session;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<PracticeSession?> GetSessionByIdAsync(ulong sessionId, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT ps.id, ps.user_id AS UserId, ps.title, ps.practice_mode AS PracticeMode,
                   ps.subject_id AS SubjectId, ps.topic_id AS TopicId, ps.difficulty_level AS DifficultyLevel,
                   ps.total_questions AS TotalQuestions, ps.attempted_questions AS AttemptedQuestions,
                   ps.correct_answers AS CorrectAnswers, ps.incorrect_answers AS IncorrectAnswers,
                   ps.unanswered_questions AS UnansweredQuestions, ps.score,
                   ps.accuracy_percentage AS AccuracyPercentage, ps.time_spent_seconds AS TimeSpentSeconds,
                   ps.time_limit_seconds AS TimeLimitSeconds, ps.status, ps.started_at_utc AS StartedAtUtc,
                   ps.completed_at_utc AS CompletedAtUtc, ps.activity_record_id AS ActivityRecordId,
                   s.name AS SubjectName, t.name AS TopicName
            FROM practice_sessions ps
            LEFT JOIN syllabus_subjects s ON ps.subject_id = s.id
            LEFT JOIN syllabus_topics t ON ps.topic_id = t.id
            WHERE ps.id = @sessionId AND ps.user_id = @userId;";

        var session = await connection.QuerySingleOrDefaultAsync<PracticeSession>(
            new CommandDefinition(sql, new { sessionId, userId }, cancellationToken: ct));

        if (session == null) return null;

        const string sqSql = @"
            SELECT psq.id, psq.practice_session_id AS PracticeSessionId, psq.question_id AS QuestionId,
                   psq.display_order AS DisplayOrder, psq.user_answer AS UserAnswer,
                   psq.is_answered AS IsAnswered, psq.is_correct AS IsCorrect,
                   psq.is_marked_for_review AS IsMarkedForReview, psq.time_spent_seconds AS TimeSpentSeconds,
                   psq.answered_at_utc AS AnsweredAtUtc,
                   q.id AS Q_Id, q.subject_id AS Q_SubjectId, q.module_id AS Q_ModuleId, q.topic_id AS Q_TopicId,
                   q.passage_id AS Q_PassageId, q.question_set_id AS Q_QuestionSetId,
                   q.question_type AS Q_QuestionType, q.difficulty_level AS Q_DifficultyLevel,
                   q.question_content AS Q_QuestionContent, q.question_image_url AS Q_QuestionImageUrl,
                   q.correct_answer AS Q_CorrectAnswer, q.explanation AS Q_Explanation, q.solution_steps AS Q_SolutionSteps,
                   q.estimated_seconds AS Q_EstimatedSeconds, q.default_marks AS Q_DefaultMarks,
                   q.default_penalty AS Q_DefaultPenalty
            FROM practice_session_questions psq
            JOIN questions q ON psq.question_id = q.id
            WHERE psq.practice_session_id = @sessionId
            ORDER BY psq.display_order ASC;";

        var items = await connection.QueryAsync(new CommandDefinition(sqSql, new { sessionId }, cancellationToken: ct));
        var questionItems = new List<PracticeSessionQuestion>();
        var qIds = new List<ulong>();

        foreach (var row in items)
        {
            var psq = new PracticeSessionQuestion
            {
                Id = row.id,
                PracticeSessionId = row.PracticeSessionId,
                QuestionId = row.QuestionId,
                DisplayOrder = row.DisplayOrder,
                UserAnswer = row.UserAnswer,
                IsAnswered = row.IsAnswered is bool bAns ? bAns : (row.IsAnswered != null && Convert.ToInt32(row.IsAnswered) != 0),
                IsCorrect = row.IsCorrect is bool bCor ? bCor : (row.IsCorrect == null ? null : Convert.ToInt32(row.IsCorrect) != 0),
                IsMarkedForReview = row.IsMarkedForReview is bool bRev ? bRev : (row.IsMarkedForReview != null && Convert.ToInt32(row.IsMarkedForReview) != 0),
                TimeSpentSeconds = row.TimeSpentSeconds,
                AnsweredAtUtc = row.AnsweredAtUtc,
                Question = new Question
                {
                    Id = row.Q_Id,
                    SubjectId = row.Q_SubjectId,
                    ModuleId = row.Q_ModuleId,
                    TopicId = row.Q_TopicId,
                    PassageId = row.Q_PassageId,
                    QuestionSetId = row.Q_QuestionSetId,
                    QuestionType = row.Q_QuestionType,
                    DifficultyLevel = row.Q_DifficultyLevel,
                    QuestionContent = row.Q_QuestionContent,
                    QuestionImageUrl = row.Q_QuestionImageUrl,
                    CorrectAnswer = session.Status == "Completed" ? row.Q_CorrectAnswer : string.Empty,
                    Explanation = session.Status == "Completed" ? row.Q_Explanation : null,
                    SolutionSteps = session.Status == "Completed" ? row.Q_SolutionSteps : null,
                    EstimatedSeconds = row.Q_EstimatedSeconds,
                    DefaultMarks = row.Q_DefaultMarks,
                    DefaultPenalty = row.Q_DefaultPenalty
                }
            };
            questionItems.Add(psq);
            qIds.Add(row.Q_Id);
        }

        if (qIds.Count > 0)
        {
            const string optSql = @"
                SELECT id, question_id AS QuestionId, option_key AS OptionKey, content,
                       is_correct AS IsCorrect, display_order AS DisplayOrder
                FROM question_options
                WHERE question_id IN @qIds
                ORDER BY display_order ASC;";

            var options = await connection.QueryAsync<QuestionOption>(new CommandDefinition(optSql, new { qIds }, cancellationToken: ct));
            var optMap = options.GroupBy(o => o.QuestionId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var q in questionItems)
            {
                if (q.Question != null && optMap.TryGetValue(q.QuestionId, out var opts))
                {
                    if (session.Status != "Completed")
                    {
                        foreach (var opt in opts) opt.IsCorrect = false;
                    }
                    q.Question.Options = opts;
                }
            }
        }

        session.Questions = questionItems;
        return session;
    }

    public async Task<IReadOnlyList<PracticeSession>> GetRecentSessionsAsync(ulong userId, int limit = 20, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT ps.id, ps.user_id AS UserId, ps.title, ps.practice_mode AS PracticeMode,
                   ps.subject_id AS SubjectId, ps.topic_id AS TopicId, ps.difficulty_level AS DifficultyLevel,
                   ps.total_questions AS TotalQuestions, ps.attempted_questions AS AttemptedQuestions,
                   ps.correct_answers AS CorrectAnswers, ps.incorrect_answers AS IncorrectAnswers,
                   ps.unanswered_questions AS UnansweredQuestions, ps.score,
                   ps.accuracy_percentage AS AccuracyPercentage, ps.time_spent_seconds AS TimeSpentSeconds,
                   ps.status, ps.started_at_utc AS StartedAtUtc, ps.completed_at_utc AS CompletedAtUtc,
                   s.name AS SubjectName, t.name AS TopicName
            FROM practice_sessions ps
            LEFT JOIN syllabus_subjects s ON ps.subject_id = s.id
            LEFT JOIN syllabus_topics t ON ps.topic_id = t.id
            WHERE ps.user_id = @userId
            ORDER BY ps.started_at_utc DESC
            LIMIT @limit;";

        var list = await connection.QueryAsync<PracticeSession>(new CommandDefinition(sql, new { userId, limit }, cancellationToken: ct));
        return list.ToList();
    }

    public async Task<bool> SaveSessionAnswerAsync(
        ulong sessionId, ulong questionId, string? answer, bool isMarkedForReview, int timeSpentSeconds, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE practice_session_questions
            SET user_answer = @answer,
                is_answered = (@answer IS NOT NULL AND @answer != ''),
                is_marked_for_review = @isMarkedForReview,
                time_spent_seconds = time_spent_seconds + @timeSpentSeconds,
                answered_at_utc = UTC_TIMESTAMP()
            WHERE practice_session_id = @sessionId AND question_id = @questionId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { sessionId, questionId, answer, isMarkedForReview, timeSpentSeconds }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> CompleteSessionAsync(
        PracticeSession session, List<PracticeSessionQuestion> updatedQuestions, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction();

        try
        {
            const string sql = @"
                UPDATE practice_sessions
                SET attempted_questions = @AttemptedQuestions,
                    correct_answers = @CorrectAnswers,
                    incorrect_answers = @IncorrectAnswers,
                    unanswered_questions = @UnansweredQuestions,
                    score = @Score,
                    accuracy_percentage = @AccuracyPercentage,
                    time_spent_seconds = @TimeSpentSeconds,
                    status = 'Completed',
                    completed_at_utc = UTC_TIMESTAMP(),
                    activity_record_id = @ActivityRecordId
                WHERE id = @Id AND user_id = @UserId;";

            await connection.ExecuteAsync(new CommandDefinition(sql, session, transaction: tx, cancellationToken: ct));

            const string qSql = @"
                UPDATE practice_session_questions
                SET is_correct = @IsCorrect,
                    user_answer = @UserAnswer,
                    is_answered = @IsAnswered
                WHERE practice_session_id = @PracticeSessionId AND question_id = @QuestionId;";

            foreach (var q in updatedQuestions)
            {
                await connection.ExecuteAsync(new CommandDefinition(qSql, q, transaction: tx, cancellationToken: ct));
            }

            tx.Commit();
            return true;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public class QuestionAttemptRepository : IQuestionAttemptRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public QuestionAttemptRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task RecordAttemptsBatchAsync(
        IEnumerable<QuestionAttempt> attempts, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);

        try
        {
            const string sql = @"
                INSERT INTO question_attempts
                    (user_id, question_id, source_type, source_id, submitted_answer,
                     is_correct, time_spent_seconds, marks_obtained, topic_id, created_at_utc)
                VALUES
                    (@UserId, @QuestionId, @SourceType, @SourceId, @SubmittedAnswer,
                     @IsCorrect, @TimeSpentSeconds, @MarksObtained, @TopicId, UTC_TIMESTAMP());";

            foreach (var a in attempts)
            {
                await conn.ExecuteAsync(new CommandDefinition(sql, a, transaction: transaction, cancellationToken: ct));
            }
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<IReadOnlyList<QuestionAttempt>> GetAttemptsByUserIdAsync(ulong userId, int limit = 50, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT qa.id, qa.user_id AS UserId, qa.question_id AS QuestionId,
                   qa.source_type AS SourceType, qa.source_id AS SourceId,
                   qa.submitted_answer AS SubmittedAnswer, qa.is_correct AS IsCorrect,
                   qa.time_spent_seconds AS TimeSpentSeconds, qa.marks_obtained AS MarksObtained,
                   qa.topic_id AS TopicId, qa.created_at_utc AS CreatedAtUtc
            FROM question_attempts qa
            WHERE qa.user_id = @userId
            ORDER BY qa.created_at_utc DESC
            LIMIT @limit;";

        var list = await connection.QueryAsync<QuestionAttempt>(new CommandDefinition(sql, new { userId, limit }, cancellationToken: ct));
        return list.ToList();
    }

    public async Task<IReadOnlyList<QuestionAttempt>> GetAttemptsForQuestionAsync(ulong userId, ulong questionId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT * FROM question_attempts
            WHERE user_id = @userId AND question_id = @questionId
            ORDER BY created_at_utc DESC;";

        var list = await connection.QueryAsync<QuestionAttempt>(new CommandDefinition(sql, new { userId, questionId }, cancellationToken: ct));
        return list.ToList();
    }

    public async Task<(int TotalAttempts, int CorrectAttempts, decimal AccuracyPct)> GetTopicAccuracySummaryAsync(
        ulong userId, ulong topicId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            SELECT COUNT(id) AS TotalAttempts,
                   COALESCE(SUM(CASE WHEN is_correct = 1 THEN 1 ELSE 0 END), 0) AS CorrectAttempts
            FROM question_attempts
            WHERE user_id = @userId AND topic_id = @topicId;";

        var res = await connection.QuerySingleOrDefaultAsync(new CommandDefinition(sql, new { userId, topicId }, cancellationToken: ct));
        int total = res?.TotalAttempts ?? 0;
        int correct = res?.CorrectAttempts ?? 0;
        decimal acc = total > 0 ? Math.Round(((decimal)correct / total) * 100m, 1) : 0m;
        return (total, correct, acc);
    }
}

