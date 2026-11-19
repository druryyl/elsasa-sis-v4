using ElsasaSis.Contracts.Student.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class Student : AggregateRoot<Guid>
{
    private const int MaxNisLength = 30;
    private const int MaxNisnLength = 20;

    private readonly List<StudentGuardian> _guardians = [];

    public Guid? CandidateId { get; private set; }
    public string Nis { get; private set; } = string.Empty;
    public string? Nisn { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string? Nickname { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateOnly EnrollmentDate { get; private set; }
    public StudentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<StudentGuardian> Guardians => _guardians.AsReadOnly();

    private Student()
    {
    }

    private Student(
        Guid id,
        Guid? candidateId,
        string nis,
        string? nisn,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        DateOnly enrollmentDate,
        StudentStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<StudentGuardian> guardians)
        : base(id)
    {
        CandidateId = candidateId;
        Nis = nis;
        Nisn = nisn;
        FullName = fullName;
        Nickname = nickname;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = phone;
        Email = email;
        Address = address;
        EnrollmentDate = enrollmentDate;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _guardians.AddRange(guardians);
    }

    public static Result<Student> EnrollFromCandidate(
        Candidate candidate,
        string nis,
        string? nisn = null,
        DateOnly? enrollmentDate = null,
        DateTime? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        var now = utcNow ?? DateTime.UtcNow;

        if (candidate.AdmissionStatus == AdmissionStatus.Enrolled)
        {
            return Result<Student>.Failure(StdErrors.CandidateAlreadyEnrolled(candidate.Id));
        }

        if (candidate.AdmissionStatus != AdmissionStatus.Accepted)
        {
            return Result<Student>.Failure(StdErrors.CandidateNotAcceptedForEnrollment(candidate.Id, candidate.AdmissionStatus));
        }

        // BR-STU-001: Required non-empty NIS.
        if (string.IsNullOrWhiteSpace(nis))
        {
            return Result<Student>.Failure(StdErrors.NisRequired());
        }

        var trimmedNis = nis.Trim();
        if (trimmedNis.Length > MaxNisLength)
        {
            return Result<Student>.Failure(StdErrors.NisTooLong());
        }

        // BR-STU-010: Normalize blank NISN to null so filtered unique index behaves cleanly.
        var normalizedNisn = StdDomainParsing.NormalizeOptional(nisn);
        if (normalizedNisn is not null && normalizedNisn.Length > MaxNisnLength)
        {
            return Result<Student>.Failure(StdErrors.NisnTooLong());
        }

        // BR-STU-002: Valid full name and date of birth.
        var personResult = StdDomainValidation.ValidatePersonDetails(
            candidate.FullName,
            candidate.DateOfBirth,
            candidate.Gender,
            now);
        if (personResult.IsFailure)
        {
            return Result<Student>.Failure(personResult.Error);
        }

        if (enrollmentDate.HasValue && enrollmentDate.Value == default)
        {
            return Result<Student>.Failure(StdErrors.EnrollmentDateInvalid());
        }

        // BR-STU-003 & BR-STU-007: At least one valid guardian required.
        if (candidate.Guardians.Count == 0)
        {
            return Result<Student>.Failure(StdErrors.AtLeastOneGuardianRequired());
        }

        foreach (var guardian in candidate.Guardians)
        {
            var guardianResult = StdDomainValidation.ValidateGuardian(
                guardian.FullName,
                guardian.RelationshipType,
                fullNameField: "guardians",
                relationshipField: "guardians");
            if (guardianResult.IsFailure)
            {
                return Result<Student>.Failure(guardianResult.Error);
            }
        }

        var studentId = UuidV7.NewGuid();
        var effectiveEnrollmentDate = enrollmentDate ?? DateOnly.FromDateTime(now);
        var clonedGuardians = candidate.Guardians
            .Select(g => StudentGuardian.CloneFromCandidateGuardian(studentId, g, now))
            .ToArray();

        var markResult = candidate.MarkEnrolled(studentId, now);
        if (markResult.IsFailure)
        {
            return Result<Student>.Failure(markResult.Error);
        }

        var student = new Student(
            studentId,
            candidate.Id,
            trimmedNis,
            normalizedNisn,
            candidate.FullName.Trim(),
            StdDomainParsing.NormalizeOptional(candidate.Nickname),
            candidate.DateOfBirth,
            candidate.Gender,
            StdDomainParsing.NormalizeOptional(candidate.Phone),
            StdDomainParsing.NormalizeOptional(candidate.Email),
            StdDomainParsing.NormalizeOptional(candidate.Address),
            effectiveEnrollmentDate,
            StudentStatus.Active,
            now,
            now,
            clonedGuardians);

        student.RaiseDomainEvent(new StudentEnrolledDomainEvent(
            student.Id,
            candidate.Id,
            student.Nis,
            student.FullName));

        return Result<Student>.Success(student);
    }

    public Result UpdateProfile(
        string? nisn,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        var personResult = StdDomainValidation.ValidatePersonDetails(fullName, dateOfBirth, gender, now);
        if (personResult.IsFailure)
        {
            return personResult;
        }

        var normalizedNisn = StdDomainParsing.NormalizeOptional(nisn);
        if (normalizedNisn is not null && normalizedNisn.Length > MaxNisnLength)
        {
            return Result.Failure(StdErrors.NisnTooLong());
        }

        Nisn = normalizedNisn;
        FullName = fullName.Trim();
        Nickname = StdDomainParsing.NormalizeOptional(nickname);
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = StdDomainParsing.NormalizeOptional(phone);
        Email = StdDomainParsing.NormalizeOptional(email);
        Address = StdDomainParsing.NormalizeOptional(address);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result<StudentGuardian> AddGuardian(
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone = null,
        string? email = null,
        string? address = null,
        DateTime? utcNow = null)
    {
        var validationResult = StdDomainValidation.ValidateGuardian(fullName, relationshipType);
        if (validationResult.IsFailure)
        {
            return Result<StudentGuardian>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var guardian = StudentGuardian.Create(
            Id,
            fullName,
            relationshipType,
            phone,
            email,
            address,
            now);

        _guardians.Add(guardian);
        UpdatedAt = now;

        return Result<StudentGuardian>.Success(guardian);
    }

    public Result<StudentGuardian> UpdateGuardian(
        Guid guardianId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone = null,
        string? email = null,
        string? address = null,
        DateTime? utcNow = null)
    {
        var guardian = _guardians.FirstOrDefault(g => g.Id == guardianId);
        if (guardian is null)
        {
            return Result<StudentGuardian>.Failure(StdErrors.GuardianNotFound(Id, guardianId));
        }

        var validationResult = StdDomainValidation.ValidateGuardian(fullName, relationshipType);
        if (validationResult.IsFailure)
        {
            return Result<StudentGuardian>.Failure(validationResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        guardian.UpdateDetails(fullName, relationshipType, phone, email, address, now);
        UpdatedAt = now;

        return Result<StudentGuardian>.Success(guardian);
    }

    public Result RemoveGuardian(Guid guardianId, DateTime? utcNow = null)
    {
        var guardian = _guardians.FirstOrDefault(g => g.Id == guardianId);
        if (guardian is null)
        {
            return Result.Failure(StdErrors.GuardianNotFound(Id, guardianId));
        }

        // BR-STU-003: Student must keep at least one guardian on record.
        if (_guardians.Count <= 1)
        {
            return Result.Failure(StdErrors.CannotRemoveLastGuardian(Id));
        }

        _guardians.Remove(guardian);
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        return Result.Success();
    }

    public static Student Rehydrate(
        Guid id,
        Guid? candidateId,
        string nis,
        string? nisn,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        DateOnly enrollmentDate,
        StudentStatus status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<StudentGuardian> guardians) =>
        new(
            id,
            candidateId,
            nis,
            nisn,
            fullName,
            nickname,
            dateOfBirth,
            gender,
            phone,
            email,
            address,
            enrollmentDate,
            status,
            createdAt,
            updatedAt,
            guardians);
}
