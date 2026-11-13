using System.Security.Claims;
using Bloom.Core.Exceptions;
using Bloom.Core.Interfaces;

namespace Bloom.API.Services;

/// <summary>
/// Extracts validated identity claims exclusively from the verified JWT in the current execution context.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public ulong? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) 
                      ?? user.FindFirstValue("sub");

            if (ulong.TryParse(sub, out var id))
            {
                return id;
            }

            return null;
        }
    }

    public string? Email
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Email) 
                   ?? user?.FindFirstValue("email");
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

    public ulong GetRequiredUserId()
    {
        var userId = UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated or claims are invalid.");
        }

        return userId.Value;
    }
}

