using Bloom.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace Bloom.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Resource processed successfully")
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse<T>> CreatedSuccess<T>(T data, string message = "Resource created successfully")
    {
        return StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Created(data, message));
    }

    protected ActionResult<ApiResponse> Success(string message = "Operation completed successfully")
    {
        return Ok(ApiResponse.Ok(message));
    }

    protected ActionResult<ApiResponse<T>> Failure<T>(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
    {
        return StatusCode(statusCode, ApiResponse<T>.Fail(message, statusCode, errors));
    }

    protected ActionResult<ApiResponse> Failure(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, ApiResponse.Fail(message, statusCode));
    }
}

