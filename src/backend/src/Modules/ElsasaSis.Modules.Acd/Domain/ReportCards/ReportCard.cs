using ElsasaSis.Contracts.Academic.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.ReportCards;

public sealed class ReportCard : AggregateRoot<Guid>
{
    private readonly List<ReportCardEntry> _entries = new();

    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public string? StudentNisn { get; private set; }
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid OperationalPeriodId { get; private set; }
    public string OperationalPeriodName { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public Guid? HomeroomTeacherId { get; private set; }
    public string? HomeroomTeacherName { get; private set; }
    public string Status { get; private set; } = ReportCardStatus.Draft;
    public string? OverrideReason { get; private set; }
    public string? GeneralRemarks { get; private set; }
    public DateTime? FinalizedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<ReportCardEntry> Entries => _entries.AsReadOnly();

    private ReportCard()
    {
    }

    private ReportCard(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        string? studentNisn,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        string status,
        string? overrideReason,
        string? generalRemarks,
        DateTime? finalizedAt,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<ReportCardEntry>? entries = null)
        : base(id)
    {
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        StudentNisn = studentNisn;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        HomeroomTeacherId = homeroomTeacherId;
        HomeroomTeacherName = homeroomTeacherName;
        Status = status;
        OverrideReason = overrideReason;
        GeneralRemarks = generalRemarks;
        FinalizedAt = finalizedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        if (entries is not null)
        {
            _entries.AddRange(entries);
        }
    }

    public static Result<ReportCard> CreateDraft(
        Guid studentId,
        string studentName,
        string studentNis,
        string? studentNisn,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        Guid? id = null,
        DateTime? utcNow = null)
    {
        if (studentId == Guid.Empty || string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<ReportCard>.Failure(AcdErrors.ReportCards.StudentIdRequired());
        }

        if (academicYearId == Guid.Empty || string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<ReportCard>.Failure(AcdErrors.ReportCards.AcademicYearRequired());
        }

        if (operationalPeriodId == Guid.Empty || string.IsNullOrWhiteSpace(operationalPeriodName))
        {
            return Result<ReportCard>.Failure(AcdErrors.ReportCards.OperationalPeriodRequired());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var cardId = id.HasValue && id.Value != Guid.Empty ? id.Value : UuidV7.NewGuid();

        var reportCard = new ReportCard(
            cardId,
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            string.IsNullOrWhiteSpace(studentNisn) ? null : studentNisn.Trim(),
            academicYearId,
            academicYearName.Trim(),
            operationalPeriodId,
            operationalPeriodName.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            homeroomTeacherId,
            string.IsNullOrWhiteSpace(homeroomTeacherName) ? null : homeroomTeacherName.Trim(),
            ReportCardStatus.Draft,
            null,
            null,
            null,
            now,
            now);

        return Result<ReportCard>.Success(reportCard);
    }

    public static ReportCard Rehydrate(
        Guid id,
        Guid studentId,
        string studentName,
        string studentNis,
        string? studentNisn,
        Guid academicYearId,
        string academicYearName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid? studyGroupId,
        string? studyGroupName,
        Guid? homeroomTeacherId,
        string? homeroomTeacherName,
        string status,
        string? overrideReason,
        string? generalRemarks,
        DateTime? finalizedAt,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<ReportCardEntry>? entries = null)
    {
        return new ReportCard(
            id,
            studentId,
            studentName,
            studentNis,
            studentNisn,
            academicYearId,
            academicYearName,
            operationalPeriodId,
            operationalPeriodName,
            studyGroupId,
            studyGroupName,
            homeroomTeacherId,
            homeroomTeacherName,
            status,
            overrideReason,
            generalRemarks,
            finalizedAt,
            createdAt,
            updatedAt,
            entries);
    }

    public void AddEntry(ReportCardEntry entry)
    {
        _entries.Add(entry);
    }

    public Result Finalize(DateTime utcNow)
    {
        if (Status != ReportCardStatus.Draft)
        {
            return Result.Failure(AcdErrors.ReportCards.AlreadyFinalized(Id));
        }

        Status = ReportCardStatus.Finalized;
        FinalizedAt = utcNow;
        UpdatedAt = utcNow;

        RaiseDomainEvent(new ReportCardFinalizedDomainEvent(Id, StudentId, OperationalPeriodId, utcNow));
        return Result.Success();
    }

    public Result OverrideToDraft(string reason, DateTime utcNow)
    {
        if (Status != ReportCardStatus.Finalized)
        {
            return Result.Failure(AcdErrors.ReportCards.NotFinalized(Id));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(AcdErrors.ReportCards.EmptyOverrideReason());
        }

        Status = ReportCardStatus.Draft;
        FinalizedAt = null;
        OverrideReason = reason.Trim();
        UpdatedAt = utcNow;

        return Result.Success();
    }

    public Result UpdateHomeroomRemarks(string remarks, DateTime utcNow)
    {
        if (Status != ReportCardStatus.Draft)
        {
            return Result.Failure(AcdErrors.ReportCards.AlreadyFinalized(Id));
        }

        GeneralRemarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim();
        UpdatedAt = utcNow;

        return Result.Success();
    }
}
