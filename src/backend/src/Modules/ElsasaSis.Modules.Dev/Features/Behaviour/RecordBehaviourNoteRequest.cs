namespace ElsasaSis.Modules.Dev.Features.Behaviour;

public sealed record RecordBehaviourNoteRequest(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    string BehaviourType,
    DateOnly ObservedDate,
    string Description,
    Guid? RecorderId = null,
    string? RecorderName = null);
