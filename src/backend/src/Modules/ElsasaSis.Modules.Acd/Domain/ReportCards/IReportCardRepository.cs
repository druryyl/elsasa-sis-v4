namespace ElsasaSis.Modules.Acd.Domain.ReportCards;

public sealed record StudentDraftEntryData(
    Guid StudentId,
    Guid SubjectId,
    string SubjectName,
    string? SubjectCode,
    Guid SubjectStageId,
    string SubjectStageName,
    Guid? TeacherId,
    string? TeacherName,
    decimal NumericScore);

public interface IReportCardRepository
{
    Task<ReportCard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReportCard?> GetByStudentAndPeriodAsync(Guid studentId, Guid periodId, CancellationToken ct = default);
    Task<IReadOnlyList<ReportCardEntry>> GetEntriesByReportCardIdAsync(Guid reportCardId, CancellationToken ct = default);
    Task InsertAsync(ReportCard reportCard, IEnumerable<ReportCardEntry> entries, CancellationToken ct = default);
    Task InsertBatchAsync(IEnumerable<(ReportCard ReportCard, IEnumerable<ReportCardEntry> Entries)> items, CancellationToken ct = default);
    Task UpdateAsync(ReportCard reportCard, CancellationToken ct = default);
    Task UpdateEntryNotesAsync(ReportCardEntry entry, CancellationToken ct = default);

    Task<IReadOnlySet<Guid>> GetExistingCardStudentIdsAsync(
        Guid operationalPeriodId,
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<Guid, string?>> GetStudentNisnsAsync(
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default);

    Task<IReadOnlyList<StudentDraftEntryData>> ComputeDraftCardEntriesAsync(
        Guid studyGroupId,
        Guid operationalPeriodId,
        string schoolLevel,
        string grade,
        IEnumerable<Guid> studentIds,
        CancellationToken ct = default);

    Task<bool> HasTeacherAssignmentAsync(
        Guid teacherId,
        Guid studyGroupId,
        Guid subjectId,
        Guid operationalPeriodId,
        CancellationToken ct = default);
}
