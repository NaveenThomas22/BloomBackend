using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class ActivityCategoryRepository : IActivityCategoryRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ActivityCategoryRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<ActivityCategory>> GetCategoriesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, category_group AS CategoryGroup, is_active AS IsActive,
                   display_order AS DisplayOrder, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM activity_categories
            WHERE (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = 1)
            ORDER BY display_order ASC, category_name ASC;";

        var results = await connection.QueryAsync<ActivityCategory>(new CommandDefinition(
            sql, new { userId, includeInactive }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<ActivityCategory?> GetCategoryByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, category_group AS CategoryGroup, is_active AS IsActive,
                   display_order AS DisplayOrder, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM activity_categories
            WHERE id = @id AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<ActivityCategory>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<ActivityCategory?> GetCategoryByCodeAsync(string code, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, category_code AS CategoryCode, category_name AS CategoryName,
                   description, icon, color, category_group AS CategoryGroup, is_active AS IsActive,
                   display_order AS DisplayOrder, created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM activity_categories
            WHERE category_code = @code AND (user_id IS NULL OR user_id = @userId)
            ORDER BY user_id DESC
            LIMIT 1;";

        return await connection.QuerySingleOrDefaultAsync<ActivityCategory>(new CommandDefinition(
            sql, new { code, userId }, cancellationToken: ct));
    }

    public async Task<ActivityCategory> CreateCategoryAsync(ActivityCategory category, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO activity_categories 
                (user_id, category_code, category_name, description, icon, color, category_group, is_active, display_order, created_at_utc, updated_at_utc)
            VALUES 
                (@UserId, @CategoryCode, @CategoryName, @Description, @Icon, @Color, @CategoryGroup, @IsActive, @DisplayOrder, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, category, cancellationToken: ct));
        category.Id = id;
        return category;
    }

    public async Task<bool> UpdateCategoryAsync(ActivityCategory category, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE activity_categories
            SET category_name = @CategoryName,
                description = @Description,
                icon = @Icon,
                color = @Color,
                category_group = @CategoryGroup,
                is_active = @IsActive,
                display_order = @DisplayOrder,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, category, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteCategoryAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = "DELETE FROM activity_categories WHERE id = @id AND user_id = @userId;";
        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> IsCategoryUsedAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT 1 FROM activity_records WHERE activity_category_id = @id AND user_id = @userId LIMIT 1;";

        var result = await connection.ExecuteScalarAsync<int?>(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return result.HasValue;
    }
}

