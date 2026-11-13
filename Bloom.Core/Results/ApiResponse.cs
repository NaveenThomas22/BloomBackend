namespace Bloom.Core.Results;

/// <summary>
/// Standardized JSON response envelope across all Bloom API endpoints.
/// </summary>
/// <typeparam name="T">Data payload type.</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
    public int StatusCode { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string message = "Resource processed successfully") =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 200,
            TimestampUtc = DateTime.UtcNow
        };

    public static ApiResponse<T> Created(T data, string message = "Resource created successfully") =>
        new()
        {
            Success = true,
            Data = data,
            Message = message,
            StatusCode = 201,
            TimestampUtc = DateTime.UtcNow
        };

    public static ApiResponse<T> Fail(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null) =>
        new()
        {
            Success = false,
            Data = default,
            Message = message,
            StatusCode = statusCode,
            Errors = errors,
            TimestampUtc = DateTime.UtcNow
        };
}

/// <summary>
/// Non-generic API response envelope for operations without return values.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string message = "Operation completed successfully") =>
        new()
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            TimestampUtc = DateTime.UtcNow
        };

    public static ApiResponse Fail(string message, int statusCode = 400) =>
        new()
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            TimestampUtc = DateTime.UtcNow
        };
}

