using ElsasaSis.Modules.Dev.Domain;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular;

public sealed record ExtracurricularParticipationDto(
    Guid Id,
    Guid ActivityId,
    string ActivityName,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid AcademicYearId,
    string AcademicYearName,
    string Status,
    DateOnly JoinedDate,
    DateTime? CompletedAt,
    DateTime? WithdrawnAt,
    string? LastSessionLog,
    DateOnly? LastSessionDate,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ExtracurricularParticipationDto FromDomain(ExtracurricularParticipation participation) =>
        new(
            participation.Id,
            participation.ActivityId,
            participation.ActivityName,
            participation.StudentId,
            participation.StudentName,
            participation.StudentNis,
            participation.AcademicYearId,
            participation.AcademicYearName,
            participation.Status,
            participation.JoinedDate,
            participation.CompletedAt,
            participation.WithdrawnAt,
            participation.LastSessionLog,
            participation.LastSessionDate,
            participation.CreatedAt,
            participation.UpdatedAt);
}
