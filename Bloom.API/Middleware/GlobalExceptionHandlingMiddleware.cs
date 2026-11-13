using System.Diagnostics;
using System.Text.Json;
using Bloom.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Middleware;

/// <summary>
/// Centralized exception handling middleware that catches all unhandled exceptions
/// and produces sanitized RFC 7807 ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = traceId }
        };

        switch (exception)
        {
            case ValidationException validationEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation Failed";
                problemDetails.Detail = validationEx.Message;
                problemDetails.Extensions["errors"] = validationEx.Errors;
                _logger.LogWarning(exception, "Validation error occurred for request {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            case NotFoundException notFoundEx:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Resource Not Found";
                problemDetails.Detail = notFoundEx.Message;
                _logger.LogWarning("Resource not found at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            case UnauthorizedException unauthEx:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = unauthEx.Message;
                _logger.LogWarning("Unauthorized access attempt at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = forbiddenEx.Message;
                _logger.LogWarning("Forbidden resource attempt at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            case ConflictException conflictEx:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Resource Conflict";
                problemDetails.Detail = conflictEx.Message;
                _logger.LogWarning("Resource conflict at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            case DatabaseUnavailableException dbEx:
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Status = StatusCodes.Status503ServiceUnavailable;
                problemDetails.Title = "Database Service Unavailable";
                problemDetails.Detail = "The database service is currently unavailable. Please check connectivity or configuration.";
                _logger.LogError(dbEx, "Database connectivity error at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "An Unexpected Error Occurred";
                problemDetails.Detail = _environment.IsDevelopment() 
                    ? exception.Message 
                    : "An internal server error occurred. Please contact support referencing the trace identifier.";
                
                _logger.LogError(exception, "Unhandled exception occurred at {Path}. TraceId: {TraceId}", context.Request.Path, traceId);
                break;
        }

        context.Response.ContentType = "application/problem+json";
        
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, jsonOptions));
    }
}

