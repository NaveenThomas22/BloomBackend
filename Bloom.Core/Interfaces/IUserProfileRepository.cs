using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByUserIdAsync(ulong userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);
    Task<UserPreferences?> GetPreferencesByUserIdAsync(ulong userId, CancellationToken cancellationToken = default);
    Task<bool> UpdatePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken = default);
}

