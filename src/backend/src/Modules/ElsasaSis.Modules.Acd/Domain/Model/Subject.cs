using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class Subject : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;
    public const int MaxCodeLength = 30;

    public string Name { get; private set; } = string.Empty;
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Subject()
    {
    }

    private Subject(
        Guid id,
        string name,
        string? code,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        Name = name;
        Code = code;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Subject> Create(
        string name,
        string? code = null,
        bool isActive = true,
        DateTime? utcNow = null)
    {
        var validation = Validate(name, code);
        if (validation.IsFailure)
        {
            return Result<Subject>.Failure(validation.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var subject = new Subject(
            UuidV7.NewGuid(),
            name.Trim(),
            NormalizeCode(code),
            isActive,
            now,
            now);

        return Result<Subject>.Success(subject);
    }

    public Result Update(
        string name,
        string? code = null,
        bool? isActive = null,
        DateTime? utcNow = null)
    {
        var validation = Validate(name, code);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        Code = NormalizeCode(code);
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }

        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public static Subject Rehydrate(
        Guid id,
        string name,
        string? code,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new Subject(id, name, code, isActive, createdAt, updatedAt);
    }

    private static Result Validate(string name, string? code)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(LearningStructureErrors.EmptySubjectName());
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(LearningStructureErrors.SubjectNameTooLong());
        }

        if (!string.IsNullOrWhiteSpace(code) && code.Trim().Length > MaxCodeLength)
        {
            return Result.Failure(LearningStructureErrors.SubjectCodeTooLong());
        }

        return Result.Success();
    }

    private static string? NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
}
