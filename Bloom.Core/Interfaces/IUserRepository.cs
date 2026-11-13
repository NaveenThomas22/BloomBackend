using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(ulong id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<ulong> CreateAsync(User user, UserProfile profile, UserPreferences preferences, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> UpdateLastLoginAsync(ulong userId, DateTime lastLoginUtc, CancellationToken cancellationToken = default);
}

