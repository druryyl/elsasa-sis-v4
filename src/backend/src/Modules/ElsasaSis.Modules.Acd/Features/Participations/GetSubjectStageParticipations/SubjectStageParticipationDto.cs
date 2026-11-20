namespace ElsasaSis.Modules.Acd.Features.Participations.GetSubjectStageParticipations;

public sealed record SubjectStageParticipationDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid SubjectStageId,
    string SubjectStageName,
    Guid SubjectId,
    string SubjectName,
    Guid OperationalPeriodId,
    string OperationalPeriodName,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? StudyGroupId,
    string? StudyGroupName,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
