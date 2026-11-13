namespace Bloom.Core.Exceptions;

/// <summary>
/// Root domain exception for Bloom.
/// </summary>
public abstract class BloomException : Exception
{
    public int StatusCode { get; }

    protected BloomException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }

    protected BloomException(string message, Exception innerException, int statusCode = 500) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}

public class NotFoundException : BloomException
{
    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with identifier '{key}' was not found.", 404)
    {
    }

    public NotFoundException(string message) : base(message, 404)
    {
    }
}

public class ValidationException : BloomException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors) 
        : base("One or more validation failures occurred.", 400)
    {
        Errors = errors;
    }

    public ValidationException(string field, string error) 
        : base(error, 400)
    {
        Errors = new Dictionary<string, string[]>
        {
            [field] = new[] { error }
        };
    }
}

public class UnauthorizedException : BloomException
{
    public UnauthorizedException(string message = "Authentication credentials were missing or invalid.") 
        : base(message, 401)
    {
    }
}

public class ForbiddenException : BloomException
{
    public ForbiddenException(string message = "You do not have permission to access or modify this resource.") 
        : base(message, 403)
    {
    }
}

public class ConflictException : BloomException
{
    public ConflictException(string message) 
        : base(message, 409)
    {
    }
}

public class DatabaseUnavailableException : BloomException
{
    public DatabaseUnavailableException(string message = "The database service is currently unavailable.") 
        : base(message, 503)
    {
    }

    public DatabaseUnavailableException(string message, Exception innerException) 
        : base(message, innerException, 503)
    {
    }
}

