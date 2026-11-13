using System.Security.Claims;
using Bloom.Core.Entities;

namespace Bloom.Core.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

