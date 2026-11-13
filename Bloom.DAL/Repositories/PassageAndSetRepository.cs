using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class QuestionPassageRepository : IQuestionPassageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public QuestionPassageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<QuestionPassage?> GetPassageByIdAsync(ulong id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM question_passages WHERE id = @id;";
        return await connection.QuerySingleOrDefaultAsync<QuestionPassage>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<QuestionPassage>> GetPassagesAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = activeOnly
            ? "SELECT * FROM question_passages WHERE is_active = 1 ORDER BY id DESC;"
            : "SELECT * FROM question_passages ORDER BY id DESC;";
        var results = await connection.QueryAsync<QuestionPassage>(new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<QuestionPassage> CreatePassageAsync(QuestionPassage passage, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO question_passages (title, content, word_count, genre, source_attribution, is_active, created_at_utc, updated_at_utc)
            VALUES (@Title, @Content, @WordCount, @Genre, @SourceAttribution, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, passage, cancellationToken: ct));
        passage.Id = id;
        return passage;
    }

    public async Task<bool> UpdatePassageAsync(QuestionPassage passage, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE question_passages
            SET title = @Title, content = @Content, word_count = @WordCount,
                genre = @Genre, source_attribution = @SourceAttribution,
                is_active = @IsActive, updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, passage, cancellationToken: ct));
        return rows > 0;
    }
}

public class QuestionSetRepository : IQuestionSetRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public QuestionSetRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<QuestionSet?> GetSetByIdAsync(ulong id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = "SELECT * FROM question_sets WHERE id = @id;";
        return await connection.QuerySingleOrDefaultAsync<QuestionSet>(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<QuestionSet>> GetSetsAsync(bool activeOnly = true, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        var sql = activeOnly
            ? "SELECT * FROM question_sets WHERE is_active = 1 ORDER BY id DESC;"
            : "SELECT * FROM question_sets ORDER BY id DESC;";
        var results = await connection.QueryAsync<QuestionSet>(new CommandDefinition(sql, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<QuestionSet> CreateSetAsync(QuestionSet set, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            INSERT INTO question_sets (title, context_scenario, data_payload_json, set_type, source_attribution, is_active, created_at_utc, updated_at_utc)
            VALUES (@Title, @ContextScenario, @DataPayloadJson, @SetType, @SourceAttribution, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, set, cancellationToken: ct));
        set.Id = id;
        return set;
    }

    public async Task<bool> UpdateSetAsync(QuestionSet set, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        const string sql = @"
            UPDATE question_sets
            SET title = @Title, context_scenario = @ContextScenario,
                data_payload_json = @DataPayloadJson, set_type = @SetType,
                source_attribution = @SourceAttribution, is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, set, cancellationToken: ct));
        return rows > 0;
    }
}

