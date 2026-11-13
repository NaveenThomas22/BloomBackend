using Bloom.BLL.DTOs;

namespace Bloom.BLL.Interfaces;

public interface IAuthService
{
    Task<(AuthResponseDto Response, string RawRefreshToken)> RegisterAsync(
        RegisterRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default);

    Task<(AuthResponseDto Response, string RawRefreshToken)> LoginAsync(
        LoginRequestDto request, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default);

    Task<(AuthResponseDto Response, string RawRefreshToken)> RefreshTokenAsync(
        string rawRefreshToken, 
        string? ipAddress = null, 
        CancellationToken cancellationToken = default);

    Task<bool> LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
    Task<int> LogoutAllAsync(ulong userId, CancellationToken cancellationToken = default);
    Task<UserDto> GetCurrentUserAsync(ulong userId, CancellationToken cancellationToken = default);
}

