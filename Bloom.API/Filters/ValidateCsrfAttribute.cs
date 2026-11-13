using Bloom.Core.Constants;
using Bloom.Core.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bloom.API.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateCsrfAttribute : Attribute, IAsyncActionFilter
{
    private const string CsrfHeaderName = "X-CSRF-Protection";
    private const string RequestedWithHeaderName = "X-Requested-With";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // 1. Gather allowed origins from configuration + same-host origin
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5173", "http://localhost:3000" };

        var allowedSet = new HashSet<string>(allowedOrigins.Select(NormalizeOrigin), StringComparer.OrdinalIgnoreCase);

        // Include current host as an allowed origin for same-origin requests / reverse proxies
        var host = request.Host.Value;
        if (!string.IsNullOrWhiteSpace(host))
        {
            var scheme = request.Scheme;
            allowedSet.Add(NormalizeOrigin($"{scheme}://{host}"));
        }

        // 2. Validate Origin header if present
        if (request.Headers.TryGetValue("Origin", out var originValues) && !string.IsNullOrWhiteSpace(originValues))
        {
            var origin = originValues.ToString();
            if (!allowedSet.Contains(NormalizeOrigin(origin)))
            {
                context.Result = new ObjectResult(
                    ApiResponse.Fail($"CSRF validation failed: Origin '{origin}' is not permitted.", StatusCodes.Status403Forbidden))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }
        else if (request.Headers.TryGetValue("Referer", out var refererValues) && !string.IsNullOrWhiteSpace(refererValues))
        {
            // Validate Referer header if Origin is absent
            if (Uri.TryCreate(refererValues.ToString(), UriKind.Absolute, out var refererUri))
            {
                var refererOrigin = $"{refererUri.Scheme}://{refererUri.Authority}";
                if (!allowedSet.Contains(NormalizeOrigin(refererOrigin)))
                {
                    context.Result = new ObjectResult(
                        ApiResponse.Fail($"CSRF validation failed: Referer origin '{refererOrigin}' is not permitted.", StatusCodes.Status403Forbidden))
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
        }
        else
        {
            // If neither Origin nor Referer is present, verify custom Anti-CSRF header
            // Standard HTML forms (<form>) cannot set custom HTTP headers
            var hasCsrfHeader = request.Headers.ContainsKey(CsrfHeaderName) || request.Headers.ContainsKey(RequestedWithHeaderName);
            if (!hasCsrfHeader)
            {
                context.Result = new ObjectResult(
                    ApiResponse.Fail("CSRF validation failed: Missing Origin, Referer, or Anti-CSRF header (X-CSRF-Protection / X-Requested-With).", StatusCodes.Status403Forbidden))
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        await next();
    }

    private static string NormalizeOrigin(string origin)
    {
        return origin.Trim().TrimEnd('/');
    }
}
