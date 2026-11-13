using System.Data;

namespace Bloom.Core.Interfaces;

/// <summary>
/// Authoritative factory contract for creating managed database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new, unopened database connection.
    /// </summary>
    IDbConnection CreateConnection();

    /// <summary>
    /// Creates and asynchronously opens a database connection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open <see cref="IDbConnection"/> instance.</returns>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection succeeds; false otherwise.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

