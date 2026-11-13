using System.Data;
using Bloom.Core.Constants;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;
using Bloom.DAL.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Bloom.DAL.Connections;

/// <summary>
/// Authoritative implementation of IDbConnectionFactory utilizing MySqlConnector.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly int _connectionTimeoutSeconds;

    public DbConnectionFactory(IOptions<DatabaseOptions> databaseOptions, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentNullException.ThrowIfNull(configuration);

        // Prioritize Options pattern; fallback to IConfiguration directly (for user-secrets and env vars)
        var connStr = databaseOptions.Value?.BloomConnection;
        if (string.IsNullOrWhiteSpace(connStr))
        {
            connStr = configuration.GetConnectionString(DatabaseConstants.DefaultConnectionStringName);
        }

        _connectionString = connStr ?? string.Empty;
        _connectionTimeoutSeconds = databaseOptions.Value?.ConnectionTimeoutSeconds ?? 5;
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        EnsureConnectionStringConfigured();
        return new MySqlConnection(_connectionString);
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        EnsureConnectionStringConfigured();
        var connection = new MySqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch (MySqlException ex)
        {
            await connection.DisposeAsync();
            throw new DatabaseUnavailableException($"Failed to open MySQL database connection: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            await connection.DisposeAsync();
            throw new DatabaseUnavailableException("An unexpected error occurred while opening the database connection.", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            return false;
        }

        try
        {
            var builder = new MySqlConnectionStringBuilder(_connectionString)
            {
                ConnectionTimeout = (uint)Math.Max(1, _connectionTimeoutSeconds)
            };

            await using var connection = new MySqlConnection(builder.ConnectionString);
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_connectionTimeoutSeconds));

            await connection.OpenAsync(timeoutCts.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = _connectionTimeoutSeconds;

            var result = await command.ExecuteScalarAsync(timeoutCts.Token);
            return result != null && Convert.ToInt32(result) == 1;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureConnectionStringConfigured()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new DatabaseUnavailableException(
                $"The connection string '{DatabaseConstants.DefaultConnectionStringName}' is not configured. " +
                "Configure it via environment variables (ConnectionStrings__BloomConnection) or .NET user-secrets.");
        }
    }
}

