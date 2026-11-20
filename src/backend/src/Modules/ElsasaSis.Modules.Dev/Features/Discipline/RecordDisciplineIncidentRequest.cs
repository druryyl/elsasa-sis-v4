namespace ElsasaSis.Modules.Dev.Features.Discipline;

public sealed record RecordDisciplineIncidentRequest(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    DateOnly IncidentDate,
    string ViolationType,
    string Description,
    string Sanction,
    Guid? RecorderId = null,
    string? RecorderName = null);
