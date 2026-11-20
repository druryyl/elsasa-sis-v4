namespace ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;

public sealed record ProvideSubjectNotesRequest(
    Guid SubjectStageId,
    string TeacherNotes);
