using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.ReportCards;

public sealed class ReportCardEntry : Entity<Guid>
{
    public Guid ReportCardId { get; private set; }
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public string? SubjectCode { get; private set; }
    public Guid SubjectStageId { get; private set; }
    public string SubjectStageName { get; private set; } = string.Empty;
    public Guid? TeacherId { get; private set; }
    public string? TeacherName { get; private set; }
    public decimal NumericScore { get; private set; }
    public string? LetterGrade { get; private set; }
    public string? TeacherNotes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ReportCardEntry()
    {
    }

    private ReportCardEntry(
        Guid id,
        Guid reportCardId,
        Guid subjectId,
        string subjectName,
        string? subjectCode,
        Guid subjectStageId,
        string subjectStageName,
        Guid? teacherId,
        string? teacherName,
        decimal numericScore,
        string? letterGrade,
        string? teacherNotes,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        ReportCardId = reportCardId;
        SubjectId = subjectId;
        SubjectName = subjectName;
        SubjectCode = subjectCode;
        SubjectStageId = subjectStageId;
        SubjectStageName = subjectStageName;
        TeacherId = teacherId;
        TeacherName = teacherName;
        NumericScore = numericScore;
        LetterGrade = letterGrade;
        TeacherNotes = teacherNotes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<ReportCardEntry> Create(
        Guid reportCardId,
        Guid subjectId,
        string subjectName,
        string? subjectCode,
        Guid subjectStageId,
        string subjectStageName,
        decimal numericScore,
        string? letterGrade = null,
        Guid? teacherId = null,
        string? teacherName = null,
        string? teacherNotes = null,
        Guid? id = null,
        DateTime? utcNow = null)
    {
        if (subjectStageId == Guid.Empty || string.IsNullOrWhiteSpace(subjectStageName))
        {
            return Result<ReportCardEntry>.Failure(AcdErrors.ReportCards.SubjectStageRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var entryId = id.HasValue && id.Value != Guid.Empty ? id.Value : UuidV7.NewGuid();

        var entry = new ReportCardEntry(
            entryId,
            reportCardId,
            subjectId,
            subjectName.Trim(),
            string.IsNullOrWhiteSpace(subjectCode) ? null : subjectCode.Trim(),
            subjectStageId,
            subjectStageName.Trim(),
            teacherId,
            string.IsNullOrWhiteSpace(teacherName) ? null : teacherName.Trim(),
            numericScore,
            string.IsNullOrWhiteSpace(letterGrade) ? null : letterGrade.Trim(),
            string.IsNullOrWhiteSpace(teacherNotes) ? null : teacherNotes.Trim(),
            now,
            now);

        return Result<ReportCardEntry>.Success(entry);
    }

    public static ReportCardEntry Rehydrate(
        Guid id,
        Guid reportCardId,
        Guid subjectId,
        string subjectName,
        string? subjectCode,
        Guid subjectStageId,
        string subjectStageName,
        Guid? teacherId,
        string? teacherName,
        decimal numericScore,
        string? letterGrade,
        string? teacherNotes,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new ReportCardEntry(
            id,
            reportCardId,
            subjectId,
            subjectName,
            subjectCode,
            subjectStageId,
            subjectStageName,
            teacherId,
            teacherName,
            numericScore,
            letterGrade,
            teacherNotes,
            createdAt,
            updatedAt);
    }

    public Result UpdateTeacherNotes(Guid teacherId, string teacherName, string notes, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return Result.Failure(AcdErrors.ReportCards.EmptyTeacherNotes());
        }

        TeacherId = teacherId;
        TeacherName = teacherName.Trim();
        TeacherNotes = notes.Trim();
        UpdatedAt = utcNow;

        return Result.Success();
    }
}
