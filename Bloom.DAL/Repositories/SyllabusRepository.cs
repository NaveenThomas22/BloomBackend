using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class SyllabusRepository : ISyllabusRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SyllabusRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    #region Subjects

    public async Task<IReadOnlyList<SyllabusSubject>> GetSubjectsAsync(ulong? userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, code, name, description, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_subjects
            WHERE (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = 1)
            ORDER BY display_order ASC, name ASC;";

        var results = await connection.QueryAsync<SyllabusSubject>(new CommandDefinition(
            sql, 
            new { userId, includeInactive }, 
            cancellationToken: ct));

        return results.AsList();
    }

    public async Task<SyllabusSubject?> GetSubjectByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, code, name, description, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_subjects
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<SyllabusSubject>(new CommandDefinition(
            sql, 
            new { id, userId }, 
            cancellationToken: ct));
    }

    public async Task<SyllabusSubject> CreateSubjectAsync(SyllabusSubject subject, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO syllabus_subjects (user_id, code, name, description, display_order, is_active, created_at_utc, updated_at_utc)
            VALUES (@UserId, @Code, @Name, @Description, @DisplayOrder, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, subject, cancellationToken: ct));
        subject.Id = id;
        return subject;
    }

    public async Task<bool> UpdateSubjectAsync(SyllabusSubject subject, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        // Can only update own custom subjects or user-owned items
        const string sql = @"
            UPDATE syllabus_subjects
            SET name = @Name,
                code = @Code,
                description = @Description,
                display_order = @DisplayOrder,
                is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, subject, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteSubjectAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM syllabus_subjects WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    #endregion

    #region Modules

    public async Task<IReadOnlyList<SyllabusModule>> GetModulesBySubjectIdAsync(ulong subjectId, ulong? userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, subject_id AS SubjectId, user_id AS UserId, name, description, 
                   display_order AS DisplayOrder, is_active AS IsActive, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_modules
            WHERE subject_id = @subjectId
              AND (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = 1)
            ORDER BY display_order ASC, name ASC;";

        var results = await connection.QueryAsync<SyllabusModule>(new CommandDefinition(
            sql, 
            new { subjectId, userId, includeInactive }, 
            cancellationToken: ct));

        return results.AsList();
    }

    public async Task<SyllabusModule?> GetModuleByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, subject_id AS SubjectId, user_id AS UserId, name, description, 
                   display_order AS DisplayOrder, is_active AS IsActive, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_modules
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<SyllabusModule>(new CommandDefinition(
            sql, 
            new { id, userId }, 
            cancellationToken: ct));
    }

    public async Task<SyllabusModule> CreateModuleAsync(SyllabusModule module, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO syllabus_modules (subject_id, user_id, name, description, display_order, is_active, created_at_utc, updated_at_utc)
            VALUES (@SubjectId, @UserId, @Name, @Description, @DisplayOrder, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, module, cancellationToken: ct));
        module.Id = id;
        return module;
    }

    public async Task<bool> UpdateModuleAsync(SyllabusModule module, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE syllabus_modules
            SET name = @Name,
                description = @Description,
                display_order = @DisplayOrder,
                is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, module, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteModuleAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM syllabus_modules WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    #endregion

    #region Topics

    public async Task<IReadOnlyList<SyllabusTopic>> GetTopicsByModuleIdAsync(ulong moduleId, ulong? userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, module_id AS ModuleId, user_id AS UserId, name, description, 
                   estimated_study_hours AS EstimatedStudyHours, difficulty_level AS DifficultyLevel, 
                   importance_level AS ImportanceLevel, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_topics
            WHERE module_id = @moduleId
              AND (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = 1)
            ORDER BY display_order ASC, name ASC;";

        var results = await connection.QueryAsync<SyllabusTopic>(new CommandDefinition(
            sql, 
            new { moduleId, userId, includeInactive }, 
            cancellationToken: ct));

        return results.AsList();
    }

    public async Task<SyllabusTopic?> GetTopicByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, module_id AS ModuleId, user_id AS UserId, name, description, 
                   estimated_study_hours AS EstimatedStudyHours, difficulty_level AS DifficultyLevel, 
                   importance_level AS ImportanceLevel, display_order AS DisplayOrder, 
                   is_active AS IsActive, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_topics
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<SyllabusTopic>(new CommandDefinition(
            sql, 
            new { id, userId }, 
            cancellationToken: ct));
    }

    public async Task<SyllabusTopic> CreateTopicAsync(SyllabusTopic topic, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO syllabus_topics (module_id, user_id, name, description, estimated_study_hours, difficulty_level, importance_level, display_order, is_active, created_at_utc, updated_at_utc)
            VALUES (@ModuleId, @UserId, @Name, @Description, @EstimatedStudyHours, @DifficultyLevel, @ImportanceLevel, @DisplayOrder, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, topic, cancellationToken: ct));
        topic.Id = id;
        return topic;
    }

    public async Task<bool> UpdateTopicAsync(SyllabusTopic topic, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE syllabus_topics
            SET name = @Name,
                description = @Description,
                estimated_study_hours = @EstimatedStudyHours,
                difficulty_level = @DifficultyLevel,
                importance_level = @ImportanceLevel,
                display_order = @DisplayOrder,
                is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, topic, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteTopicAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM syllabus_topics WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    #endregion

    #region Subtopics

    public async Task<IReadOnlyList<SyllabusSubtopic>> GetSubtopicsByTopicIdAsync(ulong topicId, ulong? userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, topic_id AS TopicId, user_id AS UserId, name, description, 
                   display_order AS DisplayOrder, is_active AS IsActive, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_subtopics
            WHERE topic_id = @topicId
              AND (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = 1)
            ORDER BY display_order ASC, name ASC;";

        var results = await connection.QueryAsync<SyllabusSubtopic>(new CommandDefinition(
            sql, 
            new { topicId, userId, includeInactive }, 
            cancellationToken: ct));

        return results.AsList();
    }

    public async Task<SyllabusSubtopic?> GetSubtopicByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, topic_id AS TopicId, user_id AS UserId, name, description, 
                   display_order AS DisplayOrder, is_active AS IsActive, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_subtopics
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<SyllabusSubtopic>(new CommandDefinition(
            sql, 
            new { id, userId }, 
            cancellationToken: ct));
    }

    public async Task<SyllabusSubtopic> CreateSubtopicAsync(SyllabusSubtopic subtopic, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO syllabus_subtopics (topic_id, user_id, name, description, display_order, is_active, created_at_utc, updated_at_utc)
            VALUES (@TopicId, @UserId, @Name, @Description, @DisplayOrder, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, subtopic, cancellationToken: ct));
        subtopic.Id = id;
        return subtopic;
    }

    public async Task<bool> UpdateSubtopicAsync(SyllabusSubtopic subtopic, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE syllabus_subtopics
            SET name = @Name,
                description = @Description,
                display_order = @DisplayOrder,
                is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, subtopic, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteSubtopicAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM syllabus_subtopics WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    #endregion

    #region Resources

    public async Task<IReadOnlyList<SyllabusResource>> GetResourcesByTopicIdAsync(ulong topicId, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, topic_id AS TopicId, user_id AS UserId, title, resource_type AS ResourceType, 
                   url, notes, display_order AS DisplayOrder, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_resources
            WHERE topic_id = @topicId AND (user_id IS NULL OR user_id = @userId)
            ORDER BY display_order ASC, title ASC;";

        var results = await connection.QueryAsync<SyllabusResource>(new CommandDefinition(
            sql, 
            new { topicId, userId }, 
            cancellationToken: ct));

        return results.AsList();
    }

    public async Task<SyllabusResource?> GetResourceByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, topic_id AS TopicId, user_id AS UserId, title, resource_type AS ResourceType, 
                   url, notes, display_order AS DisplayOrder, 
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM syllabus_resources
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<SyllabusResource>(new CommandDefinition(
            sql, 
            new { id, userId }, 
            cancellationToken: ct));
    }

    public async Task<SyllabusResource> CreateResourceAsync(SyllabusResource resource, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO syllabus_resources (topic_id, user_id, title, resource_type, url, notes, display_order, created_at_utc, updated_at_utc)
            VALUES (@TopicId, @UserId, @Title, @ResourceType, @Url, @Notes, @DisplayOrder, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, resource, cancellationToken: ct));
        resource.Id = id;
        return resource;
    }

    public async Task<bool> DeleteResourceAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM syllabus_resources WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    #endregion
}

