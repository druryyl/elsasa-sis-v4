using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class Candidate : AggregateRoot<Guid>
{
    private readonly List<CandidateGuardian> _guardians = [];

    public Guid AdmissionYearId { get; private set; }
    public string AdmissionYearName { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Nickname { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public SchoolLevel TargetSchoolLevel { get; private set; }
    public string TargetGrade { get; private set; } = string.Empty;
    public AdmissionStatus AdmissionStatus { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? DecisionNotes { get; private set; }
    public Guid? EnrolledStudentId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<CandidateGuardian> Guardians => _guardians.AsReadOnly();

    private Candidate()
    {
    }

    private Candidate(
        Guid id,
        Guid admissionYearId,
        string admissionYearName,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        SchoolLevel targetSchoolLevel,
        string targetGrade,
        AdmissionStatus admissionStatus,
        DateTime registeredAt,
        DateTime? decidedAt,
        string? decisionNotes,
        Guid? enrolledStudentId,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<CandidateGuardian> guardians)
        : base(id)
    {
        AdmissionYearId = admissionYearId;
        AdmissionYearName = admissionYearName;
        FullName = fullName;
        Nickname = nickname;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Phone = phone;
        Email = email;
        Address = address;
        TargetSchoolLevel = targetSchoolLevel;
        TargetGrade = targetGrade;
        AdmissionStatus = admissionStatus;
        RegisteredAt = registeredAt;
        DecidedAt = decidedAt;
        DecisionNotes = decisionNotes;
        EnrolledStudentId = enrolledStudentId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        _guardians.AddRange(guardians);
    }

    public static Result<Candidate> Register(
        Guid admissionYearId,
        string admissionYearName,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        SchoolLevel targetSchoolLevel,
        string targetGrade,
        IReadOnlyList<GuardianInput> guardians,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        var yearResult = StdDomainValidation.ValidateAcademicYearReference(
            admissionYearId,
            admissionYearName,
            isAdmissionYear: true);
        if (yearResult.IsFailure)
        {
            return Result<Candidate>.Failure(yearResult.Error);
        }

        var personResult = StdDomainValidation.ValidatePersonDetails(fullName, dateOfBirth, gender, now);
        if (personResult.IsFailure)
        {
            return Result<Candidate>.Failure(personResult.Error);
        }

        var levelResult = StdDomainValidation.ValidateSchoolLevelAndGrade(
            targetSchoolLevel,
            targetGrade,
            schoolLevelField: "targetSchoolLevel",
            gradeField: "targetGrade");
        if (levelResult.IsFailure)
        {
            return Result<Candidate>.Failure(levelResult.Error);
        }

        var guardiansResult = StdDomainValidation.ValidateGuardianList(guardians);
        if (guardiansResult.IsFailure)
        {
            return Result<Candidate>.Failure(guardiansResult.Error);
        }

        var candidateId = UuidV7.NewGuid();
        var guardianEntities = guardians
            .Select(g => CandidateGuardian.Create(candidateId, g, now))
            .ToArray();

        var candidate = new Candidate(
            candidateId,
            admissionYearId,
            admissionYearName.Trim(),
            fullName.Trim(),
            StdDomainParsing.NormalizeOptional(nickname),
            dateOfBirth,
            gender,
            StdDomainParsing.NormalizeOptional(phone),
            StdDomainParsing.NormalizeOptional(email),
            StdDomainParsing.NormalizeOptional(address),
            targetSchoolLevel,
            targetGrade.Trim(),
            AdmissionStatus.Registered,
            registeredAt: now,
            decidedAt: null,
            decisionNotes: null,
            enrolledStudentId: null,
            createdAt: now,
            updatedAt: now,
            guardianEntities);

        return Result<Candidate>.Success(candidate);
    }

    public Result Accept(string? decisionNotes = null, DateTime? utcNow = null) =>
        Decide(AdmissionStatus.Accepted, decisionNotes, utcNow);

    public Result Reject(string? decisionNotes = null, DateTime? utcNow = null) =>
        Decide(AdmissionStatus.Rejected, decisionNotes, utcNow);

    public Result MarkEnrolled(Guid studentId, DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty)
        {
            return Result.Failure(StdErrors.StudentIdRequired());
        }

        // BR-STU-006: Once enrolled, candidate status is immutable.
        if (AdmissionStatus == AdmissionStatus.Enrolled)
        {
            return Result.Failure(StdErrors.CandidateAlreadyEnrolled(Id));
        }

        // BR-STU-005: Only ACCEPTED candidates may be enrolled.
        if (AdmissionStatus != AdmissionStatus.Accepted)
        {
            return Result.Failure(StdErrors.CandidateNotAcceptedForEnrollment(Id, AdmissionStatus));
        }

        var now = utcNow ?? DateTime.UtcNow;
        AdmissionStatus = AdmissionStatus.Enrolled;
        EnrolledStudentId = studentId;
        UpdatedAt = now;

        return Result.Success();
    }

    public static Candidate Rehydrate(
        Guid id,
        Guid admissionYearId,
        string admissionYearName,
        string fullName,
        string? nickname,
        DateOnly dateOfBirth,
        Gender gender,
        string? phone,
        string? email,
        string? address,
        SchoolLevel targetSchoolLevel,
        string targetGrade,
        AdmissionStatus admissionStatus,
        DateTime registeredAt,
        DateTime? decidedAt,
        string? decisionNotes,
        Guid? enrolledStudentId,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<CandidateGuardian> guardians) =>
        new(
            id,
            admissionYearId,
            admissionYearName,
            fullName,
            nickname,
            dateOfBirth,
            gender,
            phone,
            email,
            address,
            targetSchoolLevel,
            targetGrade,
            admissionStatus,
            registeredAt,
            decidedAt,
            decisionNotes,
            enrolledStudentId,
            createdAt,
            updatedAt,
            guardians);

    private Result Decide(AdmissionStatus targetStatus, string? decisionNotes, DateTime? utcNow)
    {
        // BR-STU-006: Enrolled candidates cannot change admission status.
        if (AdmissionStatus == AdmissionStatus.Enrolled)
        {
            return Result.Failure(StdErrors.CandidateAlreadyEnrolled(Id));
        }

        // BR-STU-009: Only REGISTERED -> ACCEPTED | REJECTED transitions are permitted.
        if (AdmissionStatus != AdmissionStatus.Registered)
        {
            return Result.Failure(StdErrors.InvalidCandidateStatusTransition(AdmissionStatus, targetStatus));
        }

        var now = utcNow ?? DateTime.UtcNow;
        AdmissionStatus = targetStatus;
        DecisionNotes = StdDomainParsing.NormalizeOptional(decisionNotes);
        DecidedAt = now;
        UpdatedAt = now;

        return Result.Success();
    }
}
