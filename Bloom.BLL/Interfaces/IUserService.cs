using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(ulong userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(ulong userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> GetPreferencesAsync(ulong userId, CancellationToken cancellationToken = default);
    Task<UserPreferencesDto> UpdatePreferencesAsync(ulong userId, UpdatePreferencesRequestDto request, CancellationToken cancellationToken = default);
}

