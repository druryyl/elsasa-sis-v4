using ElsasaSis.Contracts.Teaching.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Tch.Domain;

public sealed class TeacherAssignment : AggregateRoot<Guid>
{
    public Guid TeacherId { get; private set; }
    public string TeacherName { get; private set; } = string.Empty;
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid OperationalPeriodId { get; private set; }
    public string OperationalPeriodName { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public Guid StudyGroupId { get; private set; }
    public string StudyGroupName { get; private set; } = string.Empty;
    public AssignmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TeacherAssignment()
    {
    }

    private TeacherAssignment(
        Guid id,
        Guid teacherId,
        string teacherName,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid subjectId,
        string subjectName,
        Guid studyGroupId,
        string studyGroupName,
        AssignmentStatus status,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        TeacherId = teacherId;
        TeacherName = teacherName;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        SubjectId = subjectId;
        SubjectName = subjectName;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        Status = status;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<TeacherAssignment> Assign(
        Teacher teacher,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid subjectId,
        string subjectName,
        Guid studyGroupId,
        string studyGroupName,
        DateTime? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        var eligibilityResult = ValidateTeacherEligibility(teacher, subjectId);
        if (eligibilityResult.IsFailure)
        {
            return Result<TeacherAssignment>.Failure(eligibilityResult.Error);
        }

        var refsResult = ValidateReferences(
            academicYearId,
            academicYearName,
            operationalPeriodId,
            operationalPeriodName,
            subjectId,
            subjectName,
            studyGroupId,
            studyGroupName);

        if (refsResult.IsFailure)
        {
            return Result<TeacherAssignment>.Failure(refsResult.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var assignment = new TeacherAssignment(
            UuidV7.NewGuid(),
            teacher.Id,
            teacher.FullName,
            academicYearId,
            academicYearName.Trim(),
            operationalPeriodId,
            operationalPeriodName.Trim(),
            subjectId,
            subjectName.Trim(),
            studyGroupId,
            studyGroupName.Trim(),
            AssignmentStatus.Assigned,
            now,
            now);

        assignment.RaiseDomainEvent(new TeacherAssignedDomainEvent(
            assignment.Id,
            assignment.TeacherId,
            assignment.SubjectId,
            assignment.StudyGroupId,
            assignment.OperationalPeriodId));

        return Result<TeacherAssignment>.Success(assignment);
    }

    /// <summary>
    /// Reactivates a previously REMOVED assignment tuple while preserving UQ_teacher_assignments_teacher_subject_group_period.
    /// </summary>
    public Result Reactivate(DateTime? utcNow = null)
    {
        if (Status == AssignmentStatus.Assigned)
        {
            return Result.Failure(TchErrors.DuplicateAssignment(
                TeacherId,
                SubjectId,
                StudyGroupId,
                OperationalPeriodId));
        }

        Status = AssignmentStatus.Assigned;
        UpdatedAt = utcNow ?? DateTime.UtcNow;

        RaiseDomainEvent(new TeacherAssignedDomainEvent(
            Id,
            TeacherId,
            SubjectId,
            StudyGroupId,
            OperationalPeriodId));

        return Result.Success();
    }

    /// <summary>
    /// Reactivates a previously REMOVED assignment after enforcing teacher eligibility and refreshing snapshot names.
    /// </summary>
    public Result Reactivate(
        Teacher teacher,
        string academicYearName,
        string operationalPeriodName,
        string subjectName,
        string studyGroupName,
        DateTime? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(teacher);

        if (teacher.Id != TeacherId)
        {
            return Result.Failure(TchErrors.AssignmentTeacherMismatch(Id, teacher.Id));
        }

        if (Status == AssignmentStatus.Assigned)
        {
            return Result.Failure(TchErrors.DuplicateAssignment(
                TeacherId,
                SubjectId,
                StudyGroupId,
                OperationalPeriodId));
        }

        var eligibilityResult = ValidateTeacherEligibility(teacher, SubjectId);
        if (eligibilityResult.IsFailure)
        {
            return eligibilityResult;
        }

        var snapshotResult = ValidateSnapshots(academicYearName, operationalPeriodName, subjectName, studyGroupName);
        if (snapshotResult.IsFailure)
        {
            return snapshotResult;
        }

        TeacherName = teacher.FullName;
        AcademicYearName = academicYearName.Trim();
        OperationalPeriodName = operationalPeriodName.Trim();
        SubjectName = subjectName.Trim();
        StudyGroupName = studyGroupName.Trim();

        return Reactivate(utcNow);
    }

    public Result Remove(DateTime? utcNow = null)
    {
        if (Status == AssignmentStatus.Removed)
        {
            return Result.Failure(TchErrors.AssignmentAlreadyRemoved(Id));
        }

        Status = AssignmentStatus.Removed;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public static TeacherAssignment Rehydrate(
        Guid id,
        Guid teacherId,
        string teacherName,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid subjectId,
        string subjectName,
        Guid studyGroupId,
        string studyGroupName,
        AssignmentStatus status,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            teacherId,
            teacherName,
            academicYearId,
            academicYearName,
            operationalPeriodId,
            operationalPeriodName,
            subjectId,
            subjectName,
            studyGroupId,
            studyGroupName,
            status,
            createdAt,
            updatedAt);

    private static Result ValidateTeacherEligibility(Teacher teacher, Guid subjectId)
    {
        // BR-TCH-003: Only an active teacher may be assigned to a subject and study group.
        if (teacher.Status != TeacherStatus.Active)
        {
            return Result.Failure(TchErrors.TeacherInactiveForAssignment(teacher.Id));
        }

        if (subjectId == Guid.Empty)
        {
            return Result.Failure(TchErrors.SubjectIdRequired());
        }

        // BR-TCH-005: A teacher may only be assigned to subjects for which the teacher has a recorded Teaching Capability.
        if (!teacher.HasCapability(subjectId))
        {
            return Result.Failure(TchErrors.TeacherLacksCapability(teacher.Id, subjectId));
        }

        return Result.Success();
    }

    private static Result ValidateReferences(
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid subjectId,
        string subjectName,
        Guid studyGroupId,
        string studyGroupName)
    {
        if (academicYearId == Guid.Empty)
        {
            return Result.Failure(TchErrors.ReferenceIdRequired("academicYearId", "Academic year"));
        }

        if (operationalPeriodId == Guid.Empty)
        {
            return Result.Failure(TchErrors.ReferenceIdRequired("operationalPeriodId", "Operational period"));
        }

        if (subjectId == Guid.Empty)
        {
            return Result.Failure(TchErrors.SubjectIdRequired());
        }

        if (studyGroupId == Guid.Empty)
        {
            return Result.Failure(TchErrors.ReferenceIdRequired("studyGroupId", "Study group"));
        }

        return ValidateSnapshots(academicYearName, operationalPeriodName, subjectName, studyGroupName);
    }

    private static Result ValidateSnapshots(
        string academicYearName,
        string operationalPeriodName,
        string subjectName,
        string studyGroupName)
    {
        if (string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result.Failure(TchErrors.SnapshotNameRequired("academicYearName", "Academic year"));
        }

        if (string.IsNullOrWhiteSpace(operationalPeriodName))
        {
            return Result.Failure(TchErrors.SnapshotNameRequired("operationalPeriodName", "Operational period"));
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result.Failure(TchErrors.SubjectNameRequired());
        }

        if (string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result.Failure(TchErrors.SnapshotNameRequired("studyGroupName", "Study group"));
        }

        return Result.Success();
    }
}
