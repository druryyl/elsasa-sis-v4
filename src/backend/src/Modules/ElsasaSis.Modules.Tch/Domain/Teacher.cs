using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Tch.Domain;

public sealed class Teacher : AggregateRoot<Guid>
{
    private readonly List<TeachingCapability> _capabilities = [];

    public string FullName { get; private set; } = string.Empty;
    public string? Nickname { get; private set; }
    public string? Nip { get; private set; }
    public string? Nik { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public TeacherStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<TeachingCapability> Capabilities => _capabilities.AsReadOnly();

    private Teacher()
    {
    }

    private Teacher(
        Guid id,
        string fullName,
        string? nickname,
        string? nip,
        string? nik,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        TeacherStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<TeachingCapability>? capabilities = null)
        : base(id)
    {
        FullName = fullName;
        Nickname = nickname;
        Nip = nip;
        Nik = nik;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = phone;
        Email = email;
        Address = address;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        if (capabilities is not null)
        {
            _capabilities.AddRange(capabilities);
        }
    }

    public static Result<Teacher> Register(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nickname = null,
        string? nip = null,
        string? nik = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var validated = ValidateAndNormalize(
            fullName,
            dateOfBirth,
            gender,
            nickname,
            nip,
            nik,
            phone,
            email,
            address,
            now);

        if (validated.IsFailure)
        {
            return Result<Teacher>.Failure(validated.Error);
        }

        var profile = validated.Value!;
        var teacher = new Teacher(
            UuidV7.NewGuid(),
            profile.FullName,
            profile.Nickname,
            profile.Nip,
            profile.Nik,
            dateOfBirth,
            gender,
            profile.Phone,
            profile.Email,
            profile.Address,
            TeacherStatus.Active,
            now,
            now);

        return Result<Teacher>.Success(teacher);
    }

    public Result Update(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nickname = null,
        string? nip = null,
        string? nik = null,
        string? phone = null,
        string? email = null,
        string? address = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var validated = ValidateAndNormalize(
            fullName,
            dateOfBirth,
            gender,
            nickname,
            nip,
            nik,
            phone,
            email,
            address,
            now);

        if (validated.IsFailure)
        {
            return Result.Failure(validated.Error);
        }

        var profile = validated.Value!;
        FullName = profile.FullName;
        Nickname = profile.Nickname;
        Nip = profile.Nip;
        Nik = profile.Nik;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = profile.Phone;
        Email = profile.Email;
        Address = profile.Address;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the teacher while preserving existing assignments per BR-TCH-007.
    /// </summary>
    public Result Deactivate(DateTime? utcNow = null)
    {
        if (Status == TeacherStatus.Inactive)
        {
            return Result.Success();
        }

        Status = TeacherStatus.Inactive;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        if (Status == TeacherStatus.Active)
        {
            return Result.Success();
        }

        Status = TeacherStatus.Active;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result<TeachingCapability> AddCapability(Guid subjectId, string subjectName, DateTime? utcNow = null)
    {
        if (subjectId == Guid.Empty)
        {
            return Result<TeachingCapability>.Failure(TchErrors.SubjectIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result<TeachingCapability>.Failure(TchErrors.SubjectNameRequired());
        }

        // BR-TCH-006: A teacher may not have the same subject listed more than once.
        if (HasCapability(subjectId))
        {
            return Result<TeachingCapability>.Failure(TchErrors.DuplicateCapability(Id, subjectId));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var capability = TeachingCapability.Create(Id, subjectId, subjectName, now);
        _capabilities.Add(capability);
        UpdatedAt = now;

        return Result<TeachingCapability>.Success(capability);
    }

    public Result RemoveCapability(Guid subjectId, DateTime? utcNow = null)
    {
        var existing = _capabilities.FirstOrDefault(c => c.SubjectId == subjectId);
        if (existing is null)
        {
            return Result.Failure(TchErrors.CapabilityNotFound(Id, subjectId));
        }

        _capabilities.Remove(existing);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public bool HasCapability(Guid subjectId) =>
        _capabilities.Any(c => c.SubjectId == subjectId);

    public static Result<Gender> ParseGender(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<Gender>.Failure(TchErrors.InvalidGender(raw));
        }

        return raw.Trim().ToUpperInvariant() switch
        {
            "MALE" => Result<Gender>.Success(Gender.Male),
            "FEMALE" => Result<Gender>.Success(Gender.Female),
            _ => Result<Gender>.Failure(TchErrors.InvalidGender(raw))
        };
    }

    public static Teacher Rehydrate(
        Guid id,
        string fullName,
        string? nickname,
        string? nip,
        string? nik,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        TeacherStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<TeachingCapability>? capabilities = null) =>
        new(
            id,
            fullName,
            nickname,
            nip,
            nik,
            dateOfBirth,
            gender,
            phone,
            email,
            address,
            status,
            createdAt,
            updatedAt,
            capabilities);

    private static Result<NormalizedProfile> ValidateAndNormalize(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        string? nickname,
        string? nip,
        string? nik,
        string? phone,
        string? email,
        string? address,
        DateTime utcNow)
    {
        // BR-TCH-001: A Teacher Profile must have a full name and a date of birth.
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<NormalizedProfile>.Failure(TchErrors.FullNameRequired());
        }

        if (dateOfBirth == default)
        {
            return Result<NormalizedProfile>.Failure(TchErrors.DateOfBirthRequired());
        }

        if (dateOfBirth > DateOnly.FromDateTime(utcNow))
        {
            return Result<NormalizedProfile>.Failure(TchErrors.DateOfBirthInFuture());
        }

        // BR-TCH-008: A Teacher Profile must have a gender (male or female).
        if (!Enum.IsDefined(gender))
        {
            return Result<NormalizedProfile>.Failure(TchErrors.InvalidGender(gender.ToString()));
        }

        var normalizedPhone = NormalizeOptional(phone);
        var normalizedEmail = NormalizeOptional(email);

        // BR-TCH-002: A Teacher Profile must have at least one contact method: phone or email.
        if (normalizedPhone is null && normalizedEmail is null)
        {
            return Result<NormalizedProfile>.Failure(TchErrors.ContactMethodRequired());
        }

        return Result<NormalizedProfile>.Success(new NormalizedProfile(
            fullName.Trim(),
            NormalizeOptional(nickname),
            NormalizeOptional(nip),
            NormalizeOptional(nik),
            normalizedPhone,
            normalizedEmail,
            NormalizeOptional(address)));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record NormalizedProfile(
        string FullName,
        string? Nickname,
        string? Nip,
        string? Nik,
        string? Phone,
        string? Email,
        string? Address);
}
