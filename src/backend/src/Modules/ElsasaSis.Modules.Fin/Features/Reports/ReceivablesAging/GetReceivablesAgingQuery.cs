namespace ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;

public sealed record GetReceivablesAgingQuery(
    Guid? AcademicYearId = null,
    string? SchoolLevel = null,
    string? Grade = null,
    Guid? StudyGroupId = null,
    DateOnly? AsOfDate = null);
