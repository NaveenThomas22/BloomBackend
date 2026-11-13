using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class ExamTemplateRepository : IExamTemplateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExamTemplateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<ExamTemplate>> GetTemplatesAsync(string? examType = null, bool activeOnly = true, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        var whereClauses = new List<string>();
        var parameters = new DynamicParameters();

        if (activeOnly)
        {
            whereClauses.Add("is_active = 1 AND is_published = 1");
        }
        if (!string.IsNullOrWhiteSpace(examType))
        {
            whereClauses.Add("exam_type = @examType");
            parameters.Add("examType", examType);
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        var sql = $@"
            SELECT id, title, description, exam_type AS ExamType,
                   total_duration_minutes AS TotalDurationMinutes,
                   total_questions AS TotalQuestions, total_marks AS TotalMarks,
                   instructions, is_published AS IsPublished, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM exam_templates
            {whereSql}
            ORDER BY id ASC;";

        var templates = (await connection.QueryAsync<ExamTemplate>(new CommandDefinition(sql, parameters, cancellationToken: ct))).ToList();

        if (templates.Count > 0)
        {
            var tIds = templates.Select(t => t.Id).ToList();
            const string secSql = @"
                SELECT id, exam_template_id AS ExamTemplateId, section_name AS SectionName,
                       subject_id AS SubjectId, display_order AS DisplayOrder,
                       duration_minutes AS DurationMinutes, question_count AS QuestionCount,
                       can_navigate_back AS CanNavigateBack, correct_marks AS CorrectMarks,
                       incorrect_penalty AS IncorrectPenalty, created_at_utc AS CreatedAtUtc
                FROM exam_sections
                WHERE exam_template_id IN @tIds
                ORDER BY display_order ASC;";

            var sections = await connection.QueryAsync<ExamSection>(new CommandDefinition(secSql, new { tIds }, cancellationToken: ct));
            var secMap = sections.GroupBy(s => s.ExamTemplateId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var t in templates)
            {
                if (secMap.TryGetValue(t.Id, out var secs))
                {
                    t.Sections = secs;
                }
            }
        }

        return templates;
    }

    public async Task<ExamTemplate?> GetTemplateWithSectionsAsync(ulong templateId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, title, description, exam_type AS ExamType,
                   total_duration_minutes AS TotalDurationMinutes,
                   total_questions AS TotalQuestions, total_marks AS TotalMarks,
                   instructions, is_published AS IsPublished, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM exam_templates
            WHERE id = @templateId;";

        var template = await connection.QuerySingleOrDefaultAsync<ExamTemplate>(
            new CommandDefinition(sql, new { templateId }, cancellationToken: ct));

        if (template == null) return null;

        const string secSql = @"
            SELECT id, exam_template_id AS ExamTemplateId, section_name AS SectionName,
                   subject_id AS SubjectId, display_order AS DisplayOrder,
                   duration_minutes AS DurationMinutes, question_count AS QuestionCount,
                   can_navigate_back AS CanNavigateBack, correct_marks AS CorrectMarks,
                   incorrect_penalty AS IncorrectPenalty, created_at_utc AS CreatedAtUtc
            FROM exam_sections
            WHERE exam_template_id = @templateId
            ORDER BY display_order ASC;";

        var sections = (await connection.QueryAsync<ExamSection>(
            new CommandDefinition(secSql, new { templateId }, cancellationToken: ct))).ToList();

        if (sections.Count > 0)
        {
            var secIds = sections.Select(s => s.Id).ToList();
            const string qSql = @"
                SELECT id, exam_section_id AS ExamSectionId, question_id AS QuestionId,
                       display_order AS DisplayOrder, marks_override AS MarksOverride,
                       penalty_override AS PenaltyOverride
                FROM exam_section_questions
                WHERE exam_section_id IN @secIds
                ORDER BY display_order ASC;";

            var secQuestions = await connection.QueryAsync<ExamSectionQuestion>(
                new CommandDefinition(qSql, new { secIds }, cancellationToken: ct));

            var qMap = secQuestions.GroupBy(q => q.ExamSectionId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var s in sections)
            {
                if (qMap.TryGetValue(s.Id, out var qs)) s.SectionQuestions = qs;
            }
        }

        template.Sections = sections;
        return template;
    }

    public async Task<ExamTemplate> CreateTemplateAsync(ExamTemplate template, List<ExamSection> sections, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction();

        try
        {
            const string sql = @"
                INSERT INTO exam_templates
                    (title, description, exam_type, total_duration_minutes, total_questions, total_marks, instructions, is_published, is_active, created_at_utc, updated_at_utc)
                VALUES
                    (@Title, @Description, @ExamType, @TotalDurationMinutes, @TotalQuestions, @TotalMarks, @Instructions, @IsPublished, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();";

            var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, template, transaction: tx, cancellationToken: ct));
            template.Id = id;

            foreach (var s in sections)
            {
                s.ExamTemplateId = id;
                const string secSql = @"
                    INSERT INTO exam_sections
                        (exam_template_id, section_name, subject_id, display_order, duration_minutes, question_count, can_navigate_back, correct_marks, incorrect_penalty, created_at_utc)
                    VALUES
                        (@ExamTemplateId, @SectionName, @SubjectId, @DisplayOrder, @DurationMinutes, @QuestionCount, @CanNavigateBack, @CorrectMarks, @IncorrectPenalty, UTC_TIMESTAMP());
                    SELECT LAST_INSERT_ID();";

                var sId = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(secSql, s, transaction: tx, cancellationToken: ct));
                s.Id = sId;

                foreach (var q in s.SectionQuestions)
                {
                    q.ExamSectionId = sId;
                    const string sqSql = @"
                        INSERT INTO exam_section_questions
                            (exam_section_id, question_id, display_order, marks_override, penalty_override)
                        VALUES
                            (@ExamSectionId, @QuestionId, @DisplayOrder, @MarksOverride, @PenaltyOverride);";
                    await connection.ExecuteAsync(new CommandDefinition(sqSql, q, transaction: tx, cancellationToken: ct));
                }
            }

            tx.Commit();
            template.Sections = sections;
            return template;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateTemplateAsync(ExamTemplate template, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE exam_templates
            SET title = @Title, description = @Description, exam_type = @ExamType,
                total_duration_minutes = @TotalDurationMinutes, total_questions = @TotalQuestions,
                total_marks = @TotalMarks, instructions = @Instructions,
                is_published = @IsPublished, is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, template, cancellationToken: ct));
        return rows > 0;
    }
}

