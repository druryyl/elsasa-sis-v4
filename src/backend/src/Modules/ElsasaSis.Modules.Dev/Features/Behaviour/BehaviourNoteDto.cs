namespace ElsasaSis.Modules.Dev.Features.Behaviour;

public sealed record BehaviourNoteDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    string BehaviourType,
    DateOnly ObservedDate,
    string Description,
    Guid RecorderId,
    string RecorderName,
    DateTime CreatedAt);
