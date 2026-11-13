namespace Bloom.Core.Interfaces;

/// <summary>
/// Service contract for applying database schema migrations asynchronously.
/// </summary>
public interface IDatabaseMigrator
{
    /// <summary>
    /// Discovers and executes pending SQL migrations in sequential order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of newly applied migrations.</returns>
    Task<int> MigrateAsync(CancellationToken cancellationToken = default);
}

