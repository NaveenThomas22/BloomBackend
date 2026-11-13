using System.Data;
using Bloom.Core.Entities;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<ulong> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO refresh_tokens (
                user_id, token_hash, token_family_id, expires_at_utc,
                created_at_utc, revoked_at_utc, replaced_by_token_id,
                created_by_ip, revoked_reason
            ) VALUES (
                @UserId, @TokenHash, @TokenFamilyId, @ExpiresAtUtc,
                @CreatedAtUtc, @RevokedAtUtc, @ReplacedByTokenId,
                @CreatedByIp, @RevokedReason
            );
            SELECT LAST_INSERT_ID();";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                refreshToken.UserId,
                refreshToken.TokenHash,
                refreshToken.TokenFamilyId,
                refreshToken.ExpiresAtUtc,
                CreatedAtUtc = DateTime.UtcNow,
                refreshToken.RevokedAtUtc,
                refreshToken.ReplacedByTokenId,
                refreshToken.CreatedByIp,
                refreshToken.RevokedReason
            },
            cancellationToken: cancellationToken);

        var id = await connection.ExecuteScalarAsync<ulong>(command);
        refreshToken.Id = id;
        return id;
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                id, user_id AS UserId, token_hash AS TokenHash,
                token_family_id AS TokenFamilyId, expires_at_utc AS ExpiresAtUtc,
                created_at_utc AS CreatedAtUtc, revoked_at_utc AS RevokedAtUtc,
                replaced_by_token_id AS ReplacedByTokenId, created_by_ip AS CreatedByIp,
                revoked_reason AS RevokedReason
            FROM refresh_tokens
            WHERE token_hash = @TokenHash;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(command);
    }

    public async Task<bool> RevokeAsync(
        ulong id, 
        ulong? replacedByTokenId = null, 
        string? reason = null, 
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE refresh_tokens SET 
                revoked_at_utc = @RevokedAtUtc,
                replaced_by_token_id = @ReplacedByTokenId,
                revoked_reason = @RevokedReason
            WHERE id = @Id AND revoked_at_utc IS NULL;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                Id = id,
                RevokedAtUtc = DateTime.UtcNow,
                ReplacedByTokenId = replacedByTokenId,
                RevokedReason = reason ?? "Revoked"
            },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        return affected > 0;
    }

    public async Task<int> RevokeFamilyAsync(string tokenFamilyId, string? reason = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE refresh_tokens SET 
                revoked_at_utc = @RevokedAtUtc,
                revoked_reason = @RevokedReason
            WHERE token_family_id = @TokenFamilyId AND revoked_at_utc IS NULL;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                TokenFamilyId = tokenFamilyId,
                RevokedAtUtc = DateTime.UtcNow,
                RevokedReason = reason ?? "Token family revoked due to reuse attempt"
            },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    public async Task<int> RevokeAllForUserAsync(ulong userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE refresh_tokens SET 
                revoked_at_utc = @RevokedAtUtc,
                revoked_reason = @RevokedReason
            WHERE user_id = @UserId AND revoked_at_utc IS NULL;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new
            {
                UserId = userId,
                RevokedAtUtc = DateTime.UtcNow,
                RevokedReason = reason ?? "User requested full logout"
            },
            cancellationToken: cancellationToken);

        return await connection.ExecuteAsync(command);
    }

    public async Task<bool> RotateTokenAsync(
        ulong oldTokenId, 
        RefreshToken newToken, 
        CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Lock and verify old token is active
            const string lockSql = @"
                SELECT id, revoked_at_utc AS RevokedAtUtc 
                FROM refresh_tokens 
                WHERE id = @OldTokenId 
                FOR UPDATE;";

            var lockCommand = new CommandDefinition(lockSql, new { OldTokenId = oldTokenId }, transaction: transaction, cancellationToken: cancellationToken);
            var existing = await connection.QuerySingleOrDefaultAsync<RefreshToken>(lockCommand);

            if (existing == null || existing.RevokedAtUtc != null)
            {
                transaction.Rollback();
                return false;
            }

            // 2. Insert new token
            const string insertSql = @"
                INSERT INTO refresh_tokens (
                    user_id, token_hash, token_family_id, expires_at_utc,
                    created_at_utc, created_by_ip
                ) VALUES (
                    @UserId, @TokenHash, @TokenFamilyId, @ExpiresAtUtc,
                    @CreatedAtUtc, @CreatedByIp
                );
                SELECT LAST_INSERT_ID();";

            var insertCommand = new CommandDefinition(
                insertSql,
                new
                {
                    newToken.UserId,
                    newToken.TokenHash,
                    newToken.TokenFamilyId,
                    newToken.ExpiresAtUtc,
                    CreatedAtUtc = DateTime.UtcNow,
                    newToken.CreatedByIp
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var newId = await connection.ExecuteScalarAsync<ulong>(insertCommand);
            newToken.Id = newId;

            // 3. Mark old token revoked and linked to new token
            const string updateSql = @"
                UPDATE refresh_tokens SET 
                    revoked_at_utc = @RevokedAtUtc,
                    replaced_by_token_id = @NewId,
                    revoked_reason = 'Rotated'
                WHERE id = @OldTokenId;";

            var updateCommand = new CommandDefinition(
                updateSql,
                new
                {
                    RevokedAtUtc = DateTime.UtcNow,
                    NewId = newId,
                    OldTokenId = oldTokenId
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(updateCommand);

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

