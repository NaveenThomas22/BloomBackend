using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class RevisionPolicyRepository : IRevisionPolicyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RevisionPolicyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<IReadOnlyList<RevisionPolicy>> GetPoliciesAsync(ulong userId, bool includeInactive = false, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, policy_name AS PolicyName, description,
                   intervals_csv AS IntervalsCsv, is_system AS IsSystem, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_policies
            WHERE (user_id IS NULL OR user_id = @userId)
              AND (@includeInactive = TRUE OR is_active = TRUE)
            ORDER BY is_system DESC, policy_name ASC;";

        var results = await connection.QueryAsync<RevisionPolicy>(new CommandDefinition(
            sql, new { userId, includeInactive }, cancellationToken: ct));

        return results.AsList();
    }

    public async Task<RevisionPolicy?> GetPolicyByIdAsync(ulong id, ulong? userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, policy_name AS PolicyName, description,
                   intervals_csv AS IntervalsCsv, is_system AS IsSystem, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_policies
            WHERE id = @id
              AND (user_id IS NULL OR user_id = @userId);";

        return await connection.QuerySingleOrDefaultAsync<RevisionPolicy>(new CommandDefinition(
            sql, new { id, userId }, cancellationToken: ct));
    }

    public async Task<RevisionPolicy?> GetDefaultSystemPolicyAsync(CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            SELECT id, user_id AS UserId, policy_name AS PolicyName, description,
                   intervals_csv AS IntervalsCsv, is_system AS IsSystem, is_active AS IsActive,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM revision_policies
            WHERE is_system = TRUE AND is_active = TRUE
            ORDER BY id ASC
            LIMIT 1;";

        return await connection.QuerySingleOrDefaultAsync<RevisionPolicy>(new CommandDefinition(
            sql, cancellationToken: ct));
    }

    public async Task<RevisionPolicy> CreatePolicyAsync(RevisionPolicy policy, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            INSERT INTO revision_policies
                (user_id, policy_name, description, intervals_csv, is_system, is_active, created_at_utc, updated_at_utc)
            VALUES
                (@UserId, @PolicyName, @Description, @IntervalsCsv, @IsSystem, @IsActive, UTC_TIMESTAMP(), UTC_TIMESTAMP());
            SELECT LAST_INSERT_ID();";

        var id = await connection.ExecuteScalarAsync<ulong>(new CommandDefinition(sql, policy, cancellationToken: ct));
        policy.Id = id;
        return policy;
    }

    public async Task<bool> UpdatePolicyAsync(RevisionPolicy policy, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            UPDATE revision_policies
            SET policy_name = @PolicyName,
                description = @Description,
                intervals_csv = @IntervalsCsv,
                is_active = @IsActive,
                updated_at_utc = UTC_TIMESTAMP()
            WHERE id = @Id AND user_id = @UserId AND is_system = FALSE;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, policy, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeletePolicyAsync(ulong id, ulong userId, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);

        const string sql = @"
            DELETE FROM revision_policies
            WHERE id = @id AND user_id = @userId AND is_system = FALSE;";

        var rows = await connection.ExecuteAsync(new CommandDefinition(sql, new { id, userId }, cancellationToken: ct));
        return rows > 0;
    }
}

