using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class ExamAttemptRepository : IExamAttemptRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExamAttemptRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<ExamAttempt> CreateAttemptAsync(
        ExamAttempt attempt, List<ExamAttemptQuestion> questions,
        IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);

        try
        {
            const string sql = @"
                INSERT INTO exam_attempts
                    (user_id, exam_template_id, status, started_at_utc, deadline_utc,
                     current_section_id, section_started_at_utc, section_deadline_utc,
                     current_question_index, submitted_at_utc, total_score, max_possible_score,
                     total_questions, total_attempted, total_correct, total_incorrect,
                     total_unanswered, accuracy_percentage, time_spent_seconds, activity_record_id,
                     created_at_utc, updated_at_utc)
                VALUES
                    (@UserId, @ExamTemplateId, @Status, @StartedAtUtc, @DeadlineUtc,
                     @CurrentSectionId, @SectionStartedAtUtc, @SectionDeadlineUtc,
                     @CurrentQuestionIndex, @SubmittedAtUtc, @TotalScore, @MaxPossibleScore,
                     @TotalQuestions, @TotalAttempted, @TotalCorrect, @TotalIncorrect,
                     @TotalUnanswered, @AccuracyPercentage, @TimeSpentSeconds, @ActivityRecordId,
                     UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await conn.ExecuteScalarAsync<ulong>(
                new CommandDefinition(sql, attempt, transaction: transaction, cancellationToken: ct));
            attempt.Id = id;

            const string qSql = @"
                INSERT INTO exam_attempt_questions
                    (exam_attempt_id, exam_section_id, question_id, display_order,
                     snapshot_question_type, snapshot_content, snapshot_options_json,
                     snapshot_passage_json, snapshot_set_json, snapshot_correct_answer,
                     snapshot_marks, snapshot_penalty, snapshot_explanation, snapshot_solution_steps,
                     user_answer, answer_status, is_correct, marks_awarded, time_spent_seconds,
                     answered_at_utc, last_saved_at_utc)
                VALUES
                    (@ExamAttemptId, @ExamSectionId, @QuestionId, @DisplayOrder,
                     @SnapshotQuestionType, @SnapshotContent, @SnapshotOptionsJson,
                     @SnapshotPassageJson, @SnapshotSetJson, @SnapshotCorrectAnswer,
                     @SnapshotMarks, @SnapshotPenalty, @SnapshotExplanation, @SnapshotSolutionSteps,
                     @UserAnswer, @AnswerStatus, @IsCorrect, @MarksAwarded, @TimeSpentSeconds,
                     @AnsweredAtUtc, UTC_TIMESTAMP());";

            foreach (var q in questions)
            {
                q.ExamAttemptId = id;
                await conn.ExecuteAsync(new CommandDefinition(qSql, q, transaction: transaction, cancellationToken: ct));
            }

            attempt.AttemptQuestions = questions;
            return attempt;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<ExamAttempt?> GetAttemptByIdAsync(
        ulong attemptId, ulong userId, IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);

        try
        {
            const string sql = @"
                SELECT a.id, a.user_id AS UserId, a.exam_template_id AS ExamTemplateId,
                       a.status, a.started_at_utc AS StartedAtUtc, a.deadline_utc AS DeadlineUtc,
                       a.current_section_id AS CurrentSectionId,
                       a.section_started_at_utc AS SectionStartedAtUtc,
                       a.section_deadline_utc AS SectionDeadlineUtc,
                       a.current_question_index AS CurrentQuestionIndex,
                       a.submitted_at_utc AS SubmittedAtUtc,
                       a.total_score AS TotalScore, a.max_possible_score AS MaxPossibleScore,
                       a.total_questions AS TotalQuestions, a.total_attempted AS TotalAttempted,
                       a.total_correct AS TotalCorrect, a.total_incorrect AS TotalIncorrect,
                       a.total_unanswered AS TotalUnanswered, a.accuracy_percentage AS AccuracyPercentage,
                       a.time_spent_seconds AS TimeSpentSeconds, a.activity_record_id AS ActivityRecordId,
                       a.created_at_utc AS CreatedAtUtc, a.updated_at_utc AS UpdatedAtUtc
                FROM exam_attempts a
                WHERE a.id = @attemptId AND a.user_id = @userId;";

            var attempt = await conn.QuerySingleOrDefaultAsync<ExamAttempt>(
                new CommandDefinition(sql, new { attemptId, userId }, transaction: transaction, cancellationToken: ct));

            if (attempt == null) return null;

            // Fetch template
            const string tSql = @"
                SELECT id, title, description, exam_type AS ExamType,
                       total_duration_minutes AS TotalDurationMinutes,
                       total_questions AS TotalQuestions, total_marks AS TotalMarks,
                       instructions, is_published AS IsPublished, is_active AS IsActive
                FROM exam_templates
                WHERE id = @examTemplateId;";

            attempt.Template = await conn.QuerySingleOrDefaultAsync<ExamTemplate>(
                new CommandDefinition(tSql, new { examTemplateId = attempt.ExamTemplateId }, transaction: transaction, cancellationToken: ct));

            if (attempt.Template != null)
            {
                const string secSql = @"
                    SELECT id, exam_template_id AS ExamTemplateId, section_name AS SectionName,
                           subject_id AS SubjectId, display_order AS DisplayOrder,
                           duration_minutes AS DurationMinutes, question_count AS QuestionCount,
                           can_navigate_back AS CanNavigateBack, correct_marks AS CorrectMarks,
                           incorrect_penalty AS IncorrectPenalty
                    FROM exam_sections
                    WHERE exam_template_id = @examTemplateId
                    ORDER BY display_order ASC;";

                var sections = await conn.QueryAsync<ExamSection>(
                    new CommandDefinition(secSql, new { examTemplateId = attempt.ExamTemplateId }, transaction: transaction, cancellationToken: ct));
                attempt.Template.Sections = sections.ToList();
            }

            // Fetch attempt questions
            const string qSql = @"
                SELECT id, exam_attempt_id AS ExamAttemptId, exam_section_id AS ExamSectionId,
                       question_id AS QuestionId, display_order AS DisplayOrder,
                       snapshot_question_type AS SnapshotQuestionType,
                       snapshot_content AS SnapshotContent,
                       snapshot_options_json AS SnapshotOptionsJson,
                       snapshot_passage_json AS SnapshotPassageJson,
                       snapshot_set_json AS SnapshotSetJson,
                       snapshot_correct_answer AS SnapshotCorrectAnswer,
                       snapshot_marks AS SnapshotMarks, snapshot_penalty AS SnapshotPenalty,
                       snapshot_explanation AS SnapshotExplanation,
                       snapshot_solution_steps AS SnapshotSolutionSteps,
                       user_answer AS UserAnswer, answer_status AS AnswerStatus,
                       is_correct AS IsCorrect, marks_awarded AS MarksAwarded,
                       time_spent_seconds AS TimeSpentSeconds,
                       answered_at_utc AS AnsweredAtUtc, version AS Version, last_saved_at_utc AS LastSavedAtUtc
                FROM exam_attempt_questions
                WHERE exam_attempt_id = @attemptId
                ORDER BY exam_section_id ASC, display_order ASC;";

            var questions = await conn.QueryAsync<ExamAttemptQuestion>(
                new CommandDefinition(qSql, new { attemptId }, transaction: transaction, cancellationToken: ct));
            attempt.AttemptQuestions = questions.ToList();

            // Fetch section results if submitted
            const string rSql = @"
                SELECT id, exam_attempt_id AS ExamAttemptId, exam_section_id AS ExamSectionId,
                       section_name AS SectionName, display_order AS DisplayOrder,
                       score, max_score AS MaxScore, total_questions AS TotalQuestions,
                       attempted, correct, incorrect, unanswered,
                       accuracy_percentage AS AccuracyPercentage, time_spent_seconds AS TimeSpentSeconds
                FROM exam_attempt_section_results
                WHERE exam_attempt_id = @attemptId
                ORDER BY display_order ASC;";

            var results = await conn.QueryAsync<ExamAttemptSectionResult>(
                new CommandDefinition(rSql, new { attemptId }, transaction: transaction, cancellationToken: ct));
            attempt.SectionResults = results.ToList();

            return attempt;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<ExamAttempt?> GetActiveAttemptForUserAsync(
        ulong userId, ulong? templateId = null,
        IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);

        try
        {
            var sql = @"
                SELECT id FROM exam_attempts
                WHERE user_id = @userId AND status = 'InProgress'
                  AND (@templateId IS NULL OR exam_template_id = @templateId)
                ORDER BY started_at_utc DESC
                LIMIT 1;";

            var id = await conn.ExecuteScalarAsync<ulong?>(
                new CommandDefinition(sql, new { userId, templateId }, transaction: transaction, cancellationToken: ct));

            if (!id.HasValue) return null;
            return await GetAttemptByIdAsync(id.Value, userId, conn, transaction, ct);
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<IReadOnlyList<ExamAttempt>> GetRecentAttemptsForUserAsync(ulong userId, int limit = 20, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT a.id, a.user_id AS UserId, a.exam_template_id AS ExamTemplateId,
                   a.status, a.started_at_utc AS StartedAtUtc, a.deadline_utc AS DeadlineUtc,
                   a.submitted_at_utc AS SubmittedAtUtc, a.total_score AS TotalScore,
                   a.max_possible_score AS MaxPossibleScore, a.total_questions AS TotalQuestions,
                   a.total_attempted AS TotalAttempted, a.total_correct AS TotalCorrect,
                   a.total_incorrect AS TotalIncorrect, a.total_unanswered AS TotalUnanswered,
                   a.accuracy_percentage AS AccuracyPercentage, a.time_spent_seconds AS TimeSpentSeconds,
                   t.title AS TemplateTitle, t.exam_type AS ExamType
            FROM exam_attempts a
            JOIN exam_templates t ON a.exam_template_id = t.id
            WHERE a.user_id = @userId
            ORDER BY a.started_at_utc DESC
            LIMIT @limit;";

        var list = await connection.QueryAsync(new CommandDefinition(sql, new { userId, limit }, cancellationToken: ct));
        var resultList = new List<ExamAttempt>();

        foreach (var r in list)
        {
            resultList.Add(new ExamAttempt
            {
                Id = r.id,
                UserId = r.UserId,
                ExamTemplateId = r.ExamTemplateId,
                Status = r.status,
                StartedAtUtc = r.StartedAtUtc,
                DeadlineUtc = r.DeadlineUtc,
                SubmittedAtUtc = r.SubmittedAtUtc,
                TotalScore = r.TotalScore,
                MaxPossibleScore = r.MaxPossibleScore,
                TotalQuestions = r.TotalQuestions,
                TotalAttempted = r.TotalAttempted,
                TotalCorrect = r.TotalCorrect,
                TotalIncorrect = r.TotalIncorrect,
                TotalUnanswered = r.TotalUnanswered,
                AccuracyPercentage = r.AccuracyPercentage,
                TimeSpentSeconds = r.TimeSpentSeconds,
                Template = new ExamTemplate
                {
                    Id = r.ExamTemplateId,
                    Title = r.TemplateTitle,
                    ExamType = r.ExamType
                }
            });
        }

        return resultList;
    }

    public async Task<bool> SaveQuestionAnswerAsync(
        ulong attemptId, ulong questionId, string? answer, string answerStatus, int timeSpentSeconds, uint expectedVersion, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE exam_attempt_questions
            SET user_answer = @answer,
                answer_status = @answerStatus,
                time_spent_seconds = time_spent_seconds + @timeSpentSeconds,
                version = version + 1,
                answered_at_utc = CASE WHEN @answer IS NOT NULL AND @answer != '' THEN UTC_TIMESTAMP() ELSE answered_at_utc END,
                last_saved_at_utc = UTC_TIMESTAMP()
            WHERE exam_attempt_id = @attemptId 
              AND question_id = @questionId
              AND version = @expectedVersion;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { attemptId, questionId, answer, answerStatus, timeSpentSeconds, expectedVersion }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> TransitionSectionAsync(
        ulong attemptId, ulong nextSectionId, DateTime sectionStartTime, DateTime sectionDeadline, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE exam_attempts
            SET current_section_id = @nextSectionId,
                section_started_at_utc = @sectionStartTime,
                section_deadline_utc = @sectionDeadline,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @attemptId AND status = 'InProgress';";

        var rows = await connection.ExecuteAsync(new CommandDefinition(
            sql, new { attemptId, nextSectionId, sectionStartTime, sectionDeadline }, cancellationToken: ct));

        return rows > 0;
    }

    public async Task<bool> SubmitAttemptAsync(
        ExamAttempt attempt, List<ExamAttemptQuestion> scoredQuestions, List<ExamAttemptSectionResult> sectionResults,
        IDbConnection? connection = null, IDbTransaction? transaction = null, CancellationToken ct = default)
    {
        var shouldDispose = connection == null;
        var conn = connection ?? await _connectionFactory.CreateOpenConnectionAsync(ct);

        try
        {
            const string sql = @"
                UPDATE exam_attempts
                SET status = @Status,
                    submitted_at_utc = @SubmittedAtUtc,
                    total_score = @TotalScore,
                    max_possible_score = @MaxPossibleScore,
                    total_questions = @TotalQuestions,
                    total_attempted = @TotalAttempted,
                    total_correct = @TotalCorrect,
                    total_incorrect = @TotalIncorrect,
                    total_unanswered = @TotalUnanswered,
                    accuracy_percentage = @AccuracyPercentage,
                    time_spent_seconds = @TimeSpentSeconds,
                    activity_record_id = @ActivityRecordId,
                    updated_at_utc = UTC_TIMESTAMP()
                WHERE id = @Id AND status = 'InProgress';";

            var rows = await conn.ExecuteAsync(new CommandDefinition(sql, attempt, transaction: transaction, cancellationToken: ct));
            if (rows == 0)
            {
                // Already submitted by concurrent request / auto-submission
                return false;
            }

            const string qSql = @"
                UPDATE exam_attempt_questions
                SET is_correct = @IsCorrect,
                    marks_awarded = @MarksAwarded
                WHERE id = @Id;";

            foreach (var q in scoredQuestions)
            {
                await conn.ExecuteAsync(new CommandDefinition(qSql, q, transaction: transaction, cancellationToken: ct));
            }

            const string rSql = @"
                INSERT INTO exam_attempt_section_results
                    (exam_attempt_id, exam_section_id, section_name, display_order,
                     score, max_score, total_questions, attempted, correct, incorrect,
                     unanswered, accuracy_percentage, time_spent_seconds)
                VALUES
                    (@ExamAttemptId, @ExamSectionId, @SectionName, @DisplayOrder,
                     @Score, @MaxScore, @TotalQuestions, @Attempted, @Correct, @Incorrect,
                     @Unanswered, @AccuracyPercentage, @TimeSpentSeconds)
                ON DUPLICATE KEY UPDATE
                    score = VALUES(score),
                    attempted = VALUES(attempted),
                    correct = VALUES(correct),
                    incorrect = VALUES(incorrect),
                    unanswered = VALUES(unanswered),
                    accuracy_percentage = VALUES(accuracy_percentage);";

            foreach (var r in sectionResults)
            {
                await conn.ExecuteAsync(new CommandDefinition(rSql, r, transaction: transaction, cancellationToken: ct));
            }

            return true;
        }
        finally
        {
            if (shouldDispose) conn.Dispose();
        }
    }

    public async Task<IReadOnlyList<ExamAttempt>> GetExpiredInProgressAttemptsAsync(DateTime nowUtc, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, exam_template_id AS ExamTemplateId,
                   status, started_at_utc AS StartedAtUtc, deadline_utc AS DeadlineUtc,
                   current_section_id AS CurrentSectionId,
                   section_started_at_utc AS SectionStartedAtUtc,
                   section_deadline_utc AS SectionDeadlineUtc
            FROM exam_attempts
            WHERE status = 'InProgress' AND deadline_utc <= @nowUtc;";

        var list = await connection.QueryAsync<ExamAttempt>(new CommandDefinition(sql, new { nowUtc }, cancellationToken: ct));
        return list.ToList();
    }
}

