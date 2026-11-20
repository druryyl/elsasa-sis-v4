using ElsasaSis.Contracts.Development.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Dev.Domain;

public sealed class DisciplineRecord : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid? AcademicYearId { get; private set; }
    public string? AcademicYearName { get; private set; }
    public DateOnly IncidentDate { get; private set; }
    public string ViolationType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Sanction { get; private set; } = string.Empty;
    public Guid RecorderId { get; private set; }
    public string RecorderName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private DisciplineRecord() { }

    private DisciplineRecord(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        DateOnly incidentDate,
        string violationType,
        string description,
        string sanction,
        Guid recorderId,
        string recorderName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        IncidentDate = incidentDate;
        ViolationType = violationType;
        Description = description;
        Sanction = sanction;
        RecorderId = recorderId;
        RecorderName = recorderName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<DisciplineRecord> CreateIncident(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        DateOnly incidentDate,
        string violationType,
        string description,
        string sanction,
        Guid recorderId,
        string recorderName,
        string role,
        DateTime? utcNow = null)
    {
        var isPrincipal = string.Equals(role?.Trim(), "PRINCIPAL", StringComparison.OrdinalIgnoreCase);
        return CreateIncidentInternal(
            studentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            incidentDate,
            violationType,
            description,
            sanction,
            recorderId,
            recorderName,
            isPrincipal,
            role ?? "TEACHER",
            utcNow);
    }

    public static Result<DisciplineRecord> CreateIncident(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        DateOnly incidentDate,
        string violationType,
        string description,
        string sanction,
        Guid recorderId,
        string recorderName,
        bool isPrincipal = false,
        DateTime? utcNow = null)
    {
        return CreateIncidentInternal(
            studentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            incidentDate,
            violationType,
            description,
            sanction,
            recorderId,
            recorderName,
            isPrincipal,
            isPrincipal ? "PRINCIPAL" : "TEACHER",
            utcNow);
    }

    private static Result<DisciplineRecord> CreateIncidentInternal(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        DateOnly incidentDate,
        string violationType,
        string description,
        string sanction,
        Guid recorderId,
        string recorderName,
        bool isPrincipal,
        string role,
        DateTime? utcNow)
    {
        if (studentId == Guid.Empty)
        {
            return Result<DisciplineRecord>.Failure(DevErrors.StudentRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.StudentNameRequired());
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.StudentNisRequired());
        }

        if (string.IsNullOrWhiteSpace(violationType))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.EmptyViolationType());
        }

        if (violationType.Trim().Length > 50)
        {
            return Result<DisciplineRecord>.Failure(DevErrors.ViolationTypeTooLong(50));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.EmptyDescription());
        }

        if (description.Trim().Length > 1000)
        {
            return Result<DisciplineRecord>.Failure(DevErrors.DescriptionTooLong(1000));
        }

        if (!DisciplineSanction.IsValid(sanction))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.InvalidSanction(sanction));
        }

        var normalizedSanction = DisciplineSanction.Normalize(sanction);

        if (isPrincipal)
        {
            if (!DisciplineSanction.IsAllowedForPrincipal(normalizedSanction))
            {
                return Result<DisciplineRecord>.Failure(DevErrors.SanctionNotAllowedForRole(normalizedSanction, "PRINCIPAL"));
            }
        }
        else
        {
            if (!DisciplineSanction.IsAllowedForTeacher(normalizedSanction))
            {
                return Result<DisciplineRecord>.Failure(DevErrors.SanctionNotAllowedForRole(normalizedSanction, role));
            }
        }

        if (recorderId == Guid.Empty || string.IsNullOrWhiteSpace(recorderName))
        {
            return Result<DisciplineRecord>.Failure(DevErrors.RecorderRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var record = new DisciplineRecord(
            UuidV7.NewGuid(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            academicYearId,
            string.IsNullOrWhiteSpace(academicYearName) ? null : academicYearName.Trim(),
            incidentDate,
            violationType.Trim(),
            description.Trim(),
            normalizedSanction,
            recorderId,
            recorderName.Trim(),
            now,
            now);

        if (isPrincipal)
        {
            record.RaiseDomainEvent(new SanctionImposedDomainEvent(
                record.Id,
                record.StudentId,
                record.Sanction,
                record.IncidentDate.ToDateTime(TimeOnly.MinValue)));
        }

        return Result<DisciplineRecord>.Success(record);
    }

    public static DisciplineRecord Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        DateOnly incidentDate,
        string violationType,
        string description,
        string sanction,
        Guid recorderId,
        string recorderName,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            studentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            incidentDate,
            violationType,
            description,
            sanction,
            recorderId,
            recorderName,
            createdAt,
            updatedAt);
}
