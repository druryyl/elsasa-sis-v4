using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Dev.Domain;

public sealed class BehaviourNote : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid? AcademicYearId { get; private set; }
    public string? AcademicYearName { get; private set; }
    public string BehaviourType { get; private set; } = string.Empty;
    public DateOnly ObservedDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid RecorderId { get; private set; }
    public string RecorderName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private BehaviourNote() { }

    private BehaviourNote(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string behaviourType,
        DateOnly observedDate,
        string description,
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
        BehaviourType = behaviourType;
        ObservedDate = observedDate;
        Description = description;
        RecorderId = recorderId;
        RecorderName = recorderName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<BehaviourNote> Create(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string behaviourType,
        DateOnly observedDate,
        string description,
        Guid recorderId,
        string recorderName,
        DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty)
        {
            return Result<BehaviourNote>.Failure(DevErrors.StudentRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<BehaviourNote>.Failure(DevErrors.StudentNameRequired());
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<BehaviourNote>.Failure(DevErrors.StudentNisRequired());
        }

        if (!Domain.BehaviourType.IsValid(behaviourType))
        {
            return Result<BehaviourNote>.Failure(DevErrors.InvalidBehaviourType(behaviourType));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            return Result<BehaviourNote>.Failure(DevErrors.EmptyDescription());
        }

        if (description.Trim().Length > 1000)
        {
            return Result<BehaviourNote>.Failure(DevErrors.DescriptionTooLong(1000));
        }

        if (recorderId == Guid.Empty || string.IsNullOrWhiteSpace(recorderName))
        {
            return Result<BehaviourNote>.Failure(DevErrors.RecorderRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var note = new BehaviourNote(
            UuidV7.NewGuid(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            academicYearId,
            string.IsNullOrWhiteSpace(academicYearName) ? null : academicYearName.Trim(),
            Domain.BehaviourType.Normalize(behaviourType),
            observedDate,
            description.Trim(),
            recorderId,
            recorderName.Trim(),
            now,
            now);

        return Result<BehaviourNote>.Success(note);
    }

    public static BehaviourNote Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string behaviourType,
        DateOnly observedDate,
        string description,
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
            behaviourType,
            observedDate,
            description,
            recorderId,
            recorderName,
            createdAt,
            updatedAt);
}
