using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Dev.Domain;

public sealed class ExtracurricularActivity : AggregateRoot<Guid>
{
    private readonly List<ExtracurricularParticipation> _participations = [];

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid SupervisorId { get; private set; }
    public string SupervisorName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<ExtracurricularParticipation> Participations => _participations.AsReadOnly();

    private ExtracurricularActivity() { }

    private ExtracurricularActivity(
        Guid id,
        string name,
        string? description,
        Guid supervisorId,
        string supervisorName,
        string status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<ExtracurricularParticipation>? participations = null)
        : base(id)
    {
        Name = name;
        Description = description;
        SupervisorId = supervisorId;
        SupervisorName = supervisorName;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        if (participations is not null)
        {
            _participations.AddRange(participations);
        }
    }

    public static Result<ExtracurricularActivity> Create(
        string name,
        string? description,
        Guid supervisorId,
        string supervisorName,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ExtracurricularActivity>.Failure(DevErrors.ActivityNameRequired());
        }

        if (name.Trim().Length > 100)
        {
            return Result<ExtracurricularActivity>.Failure(DevErrors.ActivityNameTooLong(100));
        }

        if (description is not null && description.Trim().Length > 500)
        {
            return Result<ExtracurricularActivity>.Failure(DevErrors.DescriptionTooLong(500));
        }

        if (supervisorId == Guid.Empty || string.IsNullOrWhiteSpace(supervisorName))
        {
            return Result<ExtracurricularActivity>.Failure(DevErrors.SupervisorRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var activity = new ExtracurricularActivity(
            UuidV7.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            supervisorId,
            supervisorName.Trim(),
            ExtracurricularActivityStatus.Active,
            now,
            now);

        return Result<ExtracurricularActivity>.Success(activity);
    }

    public Result UpdateDetails(
        string name,
        string? description,
        Guid supervisorId,
        string supervisorName,
        string status,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(DevErrors.ActivityNameRequired());
        }

        if (name.Trim().Length > 100)
        {
            return Result.Failure(DevErrors.ActivityNameTooLong(100));
        }

        if (description is not null && description.Trim().Length > 500)
        {
            return Result.Failure(DevErrors.DescriptionTooLong(500));
        }

        if (supervisorId == Guid.Empty || string.IsNullOrWhiteSpace(supervisorName))
        {
            return Result.Failure(DevErrors.SupervisorRequired());
        }

        if (!ExtracurricularActivityStatus.IsValid(status))
        {
            return Result.Failure(DomainError.Validation(
                "Dev.Extracurricular.InvalidStatus",
                $"Status '{status}' is invalid. Allowed: ACTIVE, INACTIVE."));
        }

        var now = utcNow ?? DateTime.UtcNow;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SupervisorId = supervisorId;
        SupervisorName = supervisorName.Trim();
        Status = ExtracurricularActivityStatus.Normalize(status);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result<ExtracurricularParticipation> EnrollStudent(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid academicYearId,
        string academicYearName,
        DateOnly joinedDate,
        DateTime? utcNow = null)
    {
        if (Status != ExtracurricularActivityStatus.Active)
        {
            return Result<ExtracurricularParticipation>.Failure(DevErrors.ActivityNotActive(Id));
        }

        if (studentId == Guid.Empty)
        {
            return Result<ExtracurricularParticipation>.Failure(DevErrors.StudentRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<ExtracurricularParticipation>.Failure(DevErrors.StudentNameRequired());
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<ExtracurricularParticipation>.Failure(DevErrors.StudentNisRequired());
        }

        var alreadyEnrolled = _participations.Any(p =>
            p.StudentId == studentId &&
            p.AcademicYearId == academicYearId &&
            string.Equals(p.Status, ExtracurricularParticipationStatus.Active, StringComparison.OrdinalIgnoreCase));

        if (alreadyEnrolled)
        {
            return Result<ExtracurricularParticipation>.Failure(DevErrors.StudentAlreadyEnrolledInActivity(studentId, Id));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var participation = ExtracurricularParticipation.Create(
            Id,
            Name,
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            academicYearId,
            academicYearName.Trim(),
            joinedDate,
            now);

        _participations.Add(participation);
        UpdatedAt = now;

        return Result<ExtracurricularParticipation>.Success(participation);
    }

    public Result WithdrawStudent(Guid participationId, DateTime? utcNow = null)
    {
        var participation = _participations.FirstOrDefault(p => p.Id == participationId);
        if (participation is null)
        {
            return Result.Failure(DevErrors.ParticipationNotFound(participationId));
        }

        if (string.Equals(participation.Status, ExtracurricularParticipationStatus.Withdrawn, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(DevErrors.ParticipationAlreadyWithdrawn(participationId));
        }

        var now = utcNow ?? DateTime.UtcNow;
        participation.Withdraw(now);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result CompleteStudent(Guid participationId, DateTime? utcNow = null)
    {
        var participation = _participations.FirstOrDefault(p => p.Id == participationId);
        if (participation is null)
        {
            return Result.Failure(DevErrors.ParticipationNotFound(participationId));
        }

        var now = utcNow ?? DateTime.UtcNow;
        participation.Complete(now);
        UpdatedAt = now;

        return Result.Success();
    }

    public Result RecordSessionLog(
        Guid participationId,
        Guid supervisorId,
        DateOnly sessionDate,
        string log,
        DateTime? utcNow = null)
    {
        if (SupervisorId != supervisorId)
        {
            return Result.Failure(DevErrors.CallerNotAssignedSupervisor(supervisorId, Id));
        }

        var participation = _participations.FirstOrDefault(p => p.Id == participationId);
        if (participation is null)
        {
            return Result.Failure(DevErrors.ParticipationNotFound(participationId));
        }

        if (!string.Equals(participation.Status, ExtracurricularParticipationStatus.Active, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(DevErrors.ParticipationNotActive(participationId));
        }

        if (string.IsNullOrWhiteSpace(log))
        {
            return Result.Failure(DevErrors.EmptySessionLog());
        }

        if (log.Trim().Length > 1000)
        {
            return Result.Failure(DevErrors.SessionLogTooLong(1000));
        }

        var now = utcNow ?? DateTime.UtcNow;
        participation.RecordSessionLog(sessionDate, log.Trim(), now);
        UpdatedAt = now;

        return Result.Success();
    }

    public static ExtracurricularActivity Rehydrate(
        Guid id,
        string name,
        string? description,
        Guid supervisorId,
        string supervisorName,
        string status,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<ExtracurricularParticipation>? participations = null) =>
        new(
            id,
            name,
            description,
            supervisorId,
            supervisorName,
            status,
            createdAt,
            updatedAt,
            participations);
}
