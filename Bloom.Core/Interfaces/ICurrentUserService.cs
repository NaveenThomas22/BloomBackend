namespace Bloom.Core.Interfaces;

/// <summary>
/// Contract for extracting the validated authenticated identity from execution context.
/// Never trusts client-supplied query, route, or payload user identifiers.
/// </summary>
public interface ICurrentUserService
{
    ulong? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the authenticated UserId or throws UnauthorizedAccessException if not authenticated.
    /// </summary>
    ulong GetRequiredUserId();
}

