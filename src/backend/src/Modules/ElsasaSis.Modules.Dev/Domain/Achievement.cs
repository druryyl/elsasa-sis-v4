using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Dev.Domain;

public sealed class Achievement : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid? AcademicYearId { get; private set; }
    public string? AcademicYearName { get; private set; }
    public string AchievementType { get; private set; } = string.Empty;
    public string EventName { get; private set; } = string.Empty;
    public string RecognitionLevel { get; private set; } = string.Empty;
    public DateOnly AchievementDate { get; private set; }
    public string? Description { get; private set; }
    public Guid RecorderId { get; private set; }
    public string RecorderName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Achievement() { }

    private Achievement(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string achievementType,
        string eventName,
        string recognitionLevel,
        DateOnly achievementDate,
        string? description,
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
        AchievementType = achievementType;
        EventName = eventName;
        RecognitionLevel = recognitionLevel;
        AchievementDate = achievementDate;
        Description = description;
        RecorderId = recorderId;
        RecorderName = recorderName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Achievement> Create(
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string achievementType,
        string eventName,
        string recognitionLevel,
        DateOnly achievementDate,
        string? description,
        Guid recorderId,
        string recorderName,
        DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty)
        {
            return Result<Achievement>.Failure(DevErrors.StudentRequired());
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<Achievement>.Failure(DevErrors.StudentNameRequired());
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<Achievement>.Failure(DevErrors.StudentNisRequired());
        }

        if (!Domain.AchievementType.IsValid(achievementType))
        {
            return Result<Achievement>.Failure(DevErrors.InvalidAchievementType(achievementType));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            return Result<Achievement>.Failure(DevErrors.EmptyEventName());
        }

        if (eventName.Trim().Length > 200)
        {
            return Result<Achievement>.Failure(DevErrors.EventNameTooLong(200));
        }

        if (!Domain.RecognitionLevel.IsValid(recognitionLevel))
        {
            return Result<Achievement>.Failure(DevErrors.InvalidRecognitionLevel(recognitionLevel));
        }

        if (description is not null && description.Trim().Length > 500)
        {
            return Result<Achievement>.Failure(DevErrors.DescriptionTooLong(500));
        }

        if (recorderId == Guid.Empty || string.IsNullOrWhiteSpace(recorderName))
        {
            return Result<Achievement>.Failure(DevErrors.RecorderRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var achievement = new Achievement(
            UuidV7.NewGuid(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            academicYearId,
            string.IsNullOrWhiteSpace(academicYearName) ? null : academicYearName.Trim(),
            Domain.AchievementType.Normalize(achievementType),
            eventName.Trim(),
            Domain.RecognitionLevel.Normalize(recognitionLevel),
            achievementDate,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            recorderId,
            recorderName.Trim(),
            now,
            now);

        return Result<Achievement>.Success(achievement);
    }

    public static Achievement Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? academicYearId,
        string? academicYearName,
        string achievementType,
        string eventName,
        string recognitionLevel,
        DateOnly achievementDate,
        string? description,
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
            achievementType,
            eventName,
            recognitionLevel,
            achievementDate,
            description,
            recorderId,
            recorderName,
            createdAt,
            updatedAt);
}
