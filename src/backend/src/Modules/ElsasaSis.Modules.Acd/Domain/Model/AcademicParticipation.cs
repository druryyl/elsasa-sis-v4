using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class AcademicParticipation : AggregateRoot<Guid>
{
    public const int MaxStudentNameLength = 150;
    public const int MaxStudentNisLength = 30;
    public const int MaxSubjectStageNameLength = 100;
    public const int MaxSubjectNameLength = 100;
    public const int MaxStudyGroupNameLength = 50;
    public const int MaxOperationalPeriodNameLength = 50;
    public const int MaxAcademicYearNameLength = 50;

    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid SubjectStageId { get; private set; }
    public string SubjectStageName { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public Guid OperationalPeriodId { get; private set; }
    public string OperationalPeriodName { get; private set; } = string.Empty;
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public ParticipationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AcademicParticipation()
    {
    }

    private AcademicParticipation(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid academicYearId,
        string academicYearName,
        Guid? studyGroupId,
        string? studyGroupName,
        ParticipationStatus status,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        SubjectStageId = subjectStageId;
        SubjectStageName = subjectStageName;
        SubjectId = subjectId;
        SubjectName = subjectName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<AcademicParticipation> Create(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid academicYearId,
        string academicYearName,
        Guid? studyGroupId,
        string? studyGroupName,
        ParticipationStatus status = ParticipationStatus.Active,
        DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty)
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.StudentIdRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("studentName", "Student name"));
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("studentNis", "Student NIS"));
        }

        if (subjectStageId == Guid.Empty)
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SubjectStageIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectStageName))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("subjectStageName", "Subject stage name"));
        }

        if (subjectId == Guid.Empty)
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SubjectIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("subjectName", "Subject name"));
        }

        if (operationalPeriodId == Guid.Empty)
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.OperationalPeriodIdRequired());
        }

        if (string.IsNullOrWhiteSpace(operationalPeriodName))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("operationalPeriodName", "Operational period name"));
        }

        if (academicYearId == Guid.Empty)
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.AcademicYearIdRequired());
        }

        if (string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.SnapshotRequired("academicYearName", "Academic year name"));
        }

        if (!Enum.IsDefined(status))
        {
            return Result<AcademicParticipation>.Failure(ParticipationErrors.InvalidStatus(status.ToString()));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var participation = new AcademicParticipation(
            UuidV7.NewGuid(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            subjectStageId,
            subjectStageName.Trim(),
            subjectId,
            subjectName.Trim(),
            operationalPeriodId,
            operationalPeriodName.Trim(),
            academicYearId,
            academicYearName.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            status,
            now,
            now);

        return Result<AcademicParticipation>.Success(participation);
    }

    public Result Complete(DateTime? utcNow = null)
    {
        if (Status == ParticipationStatus.Completed)
        {
            return Result.Success();
        }

        Status = ParticipationStatus.Completed;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Reactivate(DateTime? utcNow = null)
    {
        if (Status == ParticipationStatus.Active)
        {
            return Result.Success();
        }

        Status = ParticipationStatus.Active;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateStudyGroup(Guid? studyGroupId, string? studyGroupName, DateTime? utcNow = null)
    {
        StudyGroupId = studyGroupId;
        StudyGroupName = string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim();
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public static AcademicParticipation Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid academicYearId,
        string academicYearName,
        Guid? studyGroupId,
        string? studyGroupName,
        ParticipationStatus status,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            studentId,
            studentName,
            studentNis,
            subjectStageId,
            subjectStageName,
            subjectId,
            subjectName,
            operationalPeriodId,
            operationalPeriodName,
            academicYearId,
            academicYearName,
            studyGroupId,
            studyGroupName,
            status,
            createdAt,
            updatedAt);
}
