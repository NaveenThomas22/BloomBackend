namespace Bloom.Core.Constants;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Bloom.Auth";
    public string Audience { get; set; } = "Bloom.Clients";
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
    public int RefreshTokenLifetimeDays { get; set; } = 7;
    public string RefreshTokenSameSite { get; set; } = "Strict";
}

