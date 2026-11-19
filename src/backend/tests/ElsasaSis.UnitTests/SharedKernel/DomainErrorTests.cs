using ElsasaSis.SharedKernel.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ElsasaSis.UnitTests.SharedKernel;

public sealed class DomainErrorTests
{
    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, "Resource Not Found")]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, "Business Rule Conflict")]
    [InlineData(ErrorType.Validation, StatusCodes.Status422UnprocessableEntity, "Validation Error")]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized, "Unauthorized")]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden, "Forbidden")]
    public void ToProblemDetails_MapsErrorTypeToExpectedRfc7807StatusAndTitle(
        ErrorType errorType,
        int expectedStatus,
        string expectedTitle)
    {
        var error = new DomainError("Test.Code", "Test detail message", errorType);

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblemDetails());

        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal(expectedTitle, problem.ProblemDetails.Title);
        Assert.Equal("Test detail message", problem.ProblemDetails.Detail);
        Assert.Equal("Test.Code", problem.ProblemDetails.Extensions["errorCode"]);
        Assert.False(problem.ProblemDetails.Extensions.ContainsKey("errors"));
    }

    [Fact]
    public void ToProblemDetails_WithFieldErrors_IncludesErrorsExtension()
    {
        var error = DomainError.Validation("Opr.InvalidName", "Name is required", "name");

        var problem = Assert.IsType<ProblemHttpResult>(error.ToProblemDetails());

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problem.StatusCode);
        Assert.Equal("Opr.InvalidName", problem.ProblemDetails.Extensions["errorCode"]);

        var errors = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string[]>>(
            problem.ProblemDetails.Extensions["errors"]);
        Assert.True(errors.ContainsKey("name"));
        Assert.Equal(["Name is required"], errors["name"]);
    }
}
