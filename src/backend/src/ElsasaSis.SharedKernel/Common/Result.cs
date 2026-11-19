namespace ElsasaSis.SharedKernel.Common;

public class Result
{
    protected Result(bool isSuccess, DomainError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error!;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public DomainError Error { get; }

    public static Result Success() => new(true, null);
    public static Result Failure(DomainError error) => new(false, error);

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(DomainError error) => Result<T>.Failure(error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, DomainError? error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(DomainError error) => new(false, default, error);
}
