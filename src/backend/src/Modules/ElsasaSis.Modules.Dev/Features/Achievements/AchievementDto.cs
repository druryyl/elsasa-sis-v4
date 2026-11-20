namespace ElsasaSis.Modules.Dev.Features.Achievements;

public sealed record AchievementDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    string AchievementType,
    string EventName,
    string RecognitionLevel,
    DateOnly AchievementDate,
    string? Description,
    Guid RecorderId,
    string RecorderName,
    DateTime CreatedAt);
