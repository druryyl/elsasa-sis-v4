namespace ElsasaSis.Modules.Dev.Features.Discipline;

public sealed record DisciplineRecordDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    DateOnly IncidentDate,
    string ViolationType,
    string Description,
    string Sanction,
    Guid RecorderId,
    string RecorderName,
    DateTime CreatedAt);
