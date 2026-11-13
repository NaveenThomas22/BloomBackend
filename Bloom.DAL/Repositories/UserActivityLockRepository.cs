using System.Data;
using Bloom.Core.Interfaces;
using Dapper;

namespace Bloom.DAL.Repositories;

public class UserActivityLockRepository : IUserActivityLockRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UserActivityLockRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task<T> ExecuteWithUserLockAsync<T>(ulong userId, Func<IDbConnection, IDbTransaction, Task<T>> action, CancellationToken ct = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(ct);
        using var tx = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            // Acquire exclusive row lock on the user record in InnoDB.
            // This serializes all activity/timer writes for this user while allowing full parallelism across distinct users.
            const string lockSql = "SELECT id FROM users WHERE id = @userId FOR UPDATE;";
            await connection.ExecuteAsync(new CommandDefinition(lockSql, new { userId }, transaction: tx, cancellationToken: ct));

            var result = await action(connection, tx);
            tx.Commit();
            return result;
        }
        catch
        {
            try { tx.Rollback(); } catch { /* ignore rollback exceptions */ }
            throw;
        }
    }
}
