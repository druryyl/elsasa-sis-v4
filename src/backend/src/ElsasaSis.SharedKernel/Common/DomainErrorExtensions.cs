using Microsoft.AspNetCore.Http;

namespace ElsasaSis.SharedKernel.Common;

public static class DomainErrorExtensions
{
    public static IResult ToProblemDetails(this DomainError error)
    {
        var (statusCode, title) = error.Type switch
        {
            ErrorType.NotFound     => (StatusCodes.Status404NotFound, "Resource Not Found"),
            ErrorType.Conflict     => (StatusCodes.Status409Conflict, "Business Rule Conflict"),
            ErrorType.Validation   => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ErrorType.Forbidden    => (StatusCodes.Status403Forbidden, "Forbidden"),
            _                      => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code
        };

        if (error.Errors is { Count: > 0 })
        {
            extensions["errors"] = error.Errors;
        }

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Message,
            extensions: extensions);
    }
}
