using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Bloom.Core.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Bloom.DAL.Database;

public class DatabaseMigrator : IDatabaseMigrator
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IDbConnectionFactory connectionFactory, ILogger<DatabaseMigrator> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking database schema migrations...");

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        // Acquire advisory lock to prevent concurrent migrations across multiple app instances
        const string lockName = "bloom_migration_lock";
        var lockAcquired = false;

        try
        {
            var lockResult = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition("SELECT GET_LOCK(@lockName, 60);", new { lockName }, cancellationToken: cancellationToken));

            if (lockResult != 1)
            {
                throw new InvalidOperationException("Could not acquire database migration lock within timeout.");
            }
            lockAcquired = true;

            // 1. Ensure schema_migrations table exists
            await EnsureMigrationTableExistsAsync(connection);

            // 2. Query applied migrations
            const string selectAppliedSql = "SELECT version FROM schema_migrations;";
            var appliedVersions = (await connection.QueryAsync<string>(selectAppliedSql)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 3. Find embedded SQL migrations in Bloom.DAL
            var assembly = typeof(DatabaseMigrator).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .ToList();

            var appliedCount = 0;

            foreach (var resourceName in resourceNames)
            {
                var match = Regex.Match(resourceName, @"Migrations\.(\d+)_(.+)\.sql$", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    continue;
                }

                var version = match.Groups[1].Value;
                var description = match.Groups[2].Value.Replace("_", " ");

                if (appliedVersions.Contains(version))
                {
                    continue;
                }

                _logger.LogInformation("Applying database migration {Version}: {Description}...", version, description);

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream);
                var sqlScript = await reader.ReadToEndAsync(cancellationToken);

                // Note on MySQL: DDL statements cause an implicit commit in MySQL.
                // We execute the migration script and record the version in schema_migrations.
                using var transaction = connection.BeginTransaction();
                try
                {
                    await connection.ExecuteAsync(new CommandDefinition(sqlScript, transaction: transaction, cancellationToken: cancellationToken));

                    // Record in schema_migrations
                    const string recordMigrationSql = @"
                        INSERT IGNORE INTO schema_migrations (version, description, applied_at_utc) 
                        VALUES (@version, @description, UTC_TIMESTAMP());";

                    await connection.ExecuteAsync(new CommandDefinition(recordMigrationSql, new { version, description }, transaction: transaction, cancellationToken: cancellationToken));

                    transaction.Commit();
                    appliedCount++;
                    _logger.LogInformation("Successfully applied migration {Version}.", version);
                }
                catch (Exception ex)
                {
                    try { transaction.Rollback(); } catch { /* DDL may have triggered implicit commit */ }
                    _logger.LogError(ex, "Failed applying database migration {Version}: {Message}", version, ex.Message);
                    throw;
                }
            }

            _logger.LogInformation("Database migration check completed. {Count} new migrations applied.", appliedCount);
            return appliedCount;
        }
        finally
        {
            if (lockAcquired)
            {
                try
                {
                    await connection.ExecuteScalarAsync(
                        new CommandDefinition("SELECT RELEASE_LOCK(@lockName);", new { lockName }, cancellationToken: CancellationToken.None));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to release database migration lock.");
                }
            }
        }
    }

    private static async Task EnsureMigrationTableExistsAsync(IDbConnection connection)
    {
        const string createTableSql = @"
            CREATE TABLE IF NOT EXISTS schema_migrations (
                id INT AUTO_INCREMENT PRIMARY KEY,
                version VARCHAR(100) NOT NULL UNIQUE,
                description VARCHAR(255) NOT NULL,
                applied_at_utc DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

        await connection.ExecuteAsync(createTableSql);
    }
}
