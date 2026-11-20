namespace ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;

public sealed record EnrollExtracurricularParticipantRequest(
    Guid StudentId,
    string? StudentName,
    string? StudentNis,
    Guid? AcademicYearId,
    string? AcademicYearName,
    DateOnly JoinedDate);
