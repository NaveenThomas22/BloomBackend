using System.Data;

namespace Bloom.Core.Interfaces;

/// <summary>
/// Provides atomic, database-enforced serialization for a user's activity and timer write operations.
/// Prevents concurrent overlap bypasses and race conditions.
/// </summary>
public interface IUserActivityLockRepository
{
    /// <summary>
    /// Executes the specified action inside an isolated database transaction with an exclusive row lock on the user record.
    /// Concurrent requests for the same user are serialized; requests for different users run in parallel.
    /// </summary>
    Task<T> ExecuteWithUserLockAsync<T>(ulong userId, Func<IDbConnection, IDbTransaction, Task<T>> action, CancellationToken ct = default);
}
