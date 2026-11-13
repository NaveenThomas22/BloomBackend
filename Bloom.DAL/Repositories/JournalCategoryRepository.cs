using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class JournalCategoryRepository : IJournalCategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public JournalCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<JournalCategory?> GetByIdAsync(ulong id, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, is_active AS IsActive, display_order AS DisplayOrder,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM journal_categories
            WHERE id = @id;";

        return await connection.QuerySingleOrDefaultAsync<JournalCategory>(
            new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<JournalCategory>> GetCategoriesForUserAsync(ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, is_active AS IsActive, display_order AS DisplayOrder,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM journal_categories
            WHERE (user_id IS NULL OR user_id = @userId) AND is_active = 1
            ORDER BY display_order ASC, category_name ASC;";

        var results = await connection.QueryAsync<JournalCategory>(
            new CommandDefinition(sql, new { userId }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<ulong> CreateUserCategoryAsync(JournalCategory category, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO journal_categories (user_id, category_code, category_name, description, icon, color, is_active, display_order, created_at_utc, updated_at_utc)
            VALUES (@UserId, @CategoryCode, @CategoryName, @Description, @Icon, @Color, @IsActive, @DisplayOrder, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        return await connection.ExecuteScalarAsync<ulong>(
            new CommandDefinition(sql, category, cancellationToken: ct));
    }
}

