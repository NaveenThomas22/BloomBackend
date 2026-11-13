namespace Bloom.Core.Entities;

/// <summary>
/// Domain model for tracked refresh token sessions.
/// </summary>
public class RefreshToken : BaseEntity
{
    public ulong UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string TokenFamilyId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public ulong? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsExpired && !IsRevoked;
}

