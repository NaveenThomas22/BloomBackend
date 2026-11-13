using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface IRefreshTokenRepository
{
    Task<ulong> CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(ulong id, ulong? replacedByTokenId = null, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> RevokeFamilyAsync(string tokenFamilyId, string? reason = null, CancellationToken cancellationToken = default);
    Task<int> RevokeAllForUserAsync(ulong userId, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> RotateTokenAsync(
        ulong oldTokenId, 
        RefreshToken newToken, 
        CancellationToken cancellationToken = default);
}

