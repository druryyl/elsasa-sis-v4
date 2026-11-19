using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.SharedKernel;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_SetsIsSuccessAndValue()
    {
        var result = Result<string>.Success("hello");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("hello", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_WithDomainError_SetsIsFailureAndError()
    {
        var error = DomainError.Conflict("Opr.Conflict", "Already active");
        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void NonGenericResult_SuccessAndFailure_BehaveCorrectly()
    {
        var ok = Result.Success();
        Assert.True(ok.IsSuccess);
        Assert.False(ok.IsFailure);

        var err = DomainError.NotFound("Opr.NotFound", "Missing");
        var fail = Result.Failure(err);
        Assert.False(fail.IsSuccess);
        Assert.True(fail.IsFailure);
        Assert.Equal(err, fail.Error);
    }
}
