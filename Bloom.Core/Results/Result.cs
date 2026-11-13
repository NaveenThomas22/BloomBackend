namespace Bloom.Core.Results;

/// <summary>
/// Strongly-typed Result pattern for encapsulating business logic operations.
/// </summary>
/// <typeparam name="T">Result value type.</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; }
    public int StatusCode { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, T? value, string error, int statusCode, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
        ValidationErrors = validationErrors;
    }

    public static Result<T> Success(T value, int statusCode = 200) =>
        new(true, value, string.Empty, statusCode);

    public static Result<T> Failure(string error, int statusCode = 400, IDictionary<string, string[]>? validationErrors = null) =>
        new(false, default, error, statusCode, validationErrors);
}

/// <summary>
/// Result type for operations without return data.
/// </summary>
public class Result : Result<bool>
{
    protected Result(bool isSuccess, string error, int statusCode, IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, isSuccess, error, statusCode, validationErrors)
    {
    }

    public static Result Success(int statusCode = 200) => new(true, string.Empty, statusCode);
    public static new Result Failure(string error, int statusCode = 400, IDictionary<string, string[]>? validationErrors = null) =>
        new(false, error, statusCode, validationErrors);
}

