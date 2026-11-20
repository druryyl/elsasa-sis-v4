namespace ElsasaSis.Modules.Dev.Features.Achievements;

public sealed record RecordAchievementRequest(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    string AchievementType,
    string EventName,
    string RecognitionLevel,
    DateOnly AchievementDate,
    string? Description = null,
    Guid? RecorderId = null,
    string? RecorderName = null);
