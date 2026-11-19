namespace ElsasaSis.SharedKernel.Common;

public sealed record DomainError(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static DomainError NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static DomainError Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static DomainError Validation(
        string code,
        string message,
        string? field = null) =>
        new(
            code,
            message,
            ErrorType.Validation,
            string.IsNullOrWhiteSpace(field)
                ? null
                : new Dictionary<string, string[]> { [field] = [message] });

    public static DomainError Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(code, message, ErrorType.Validation, errors);

    public static DomainError Unauthorized(string code, string message) =>
        new(code, message, ErrorType.Unauthorized);

    public static DomainError Forbidden(string code, string message) =>
        new(code, message, ErrorType.Forbidden);

    public static DomainError Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);
}
