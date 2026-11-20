using ElsasaSis.Contracts.Academic.Events;
using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Academic;

public sealed class ReportCardDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FinalizeNow = new(2026, 11, 20, 11, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OverrideNow = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid AcademicYearId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid HomeroomTeacherId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid SubjectStageId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();

    [Fact]
    public void CreateDraft_WhenValidParameters_SucceedsWithDraftState()
    {
        var result = ReportCard.CreateDraft(
            StudentId,
            " Ahmad Dahlan ",
            " 1001 ",
            " 0081234561 ",
            AcademicYearId,
            " 2026/2027 ",
            OperationalPeriodId,
            " Semester Ganjil 2026/2027 ",
            StudyGroupId,
            " 7A ",
            HomeroomTeacherId,
            " Budi Santoso ",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var card = result.Value!;
        Assert.NotEqual(Guid.Empty, card.Id);
        Assert.Equal(StudentId, card.StudentId);
        Assert.Equal("Ahmad Dahlan", card.StudentName);
        Assert.Equal("1001", card.StudentNis);
        Assert.Equal("0081234561", card.StudentNisn);
        Assert.Equal(AcademicYearId, card.AcademicYearId);
        Assert.Equal("2026/2027", card.AcademicYearName);
        Assert.Equal(OperationalPeriodId, card.OperationalPeriodId);
        Assert.Equal("Semester Ganjil 2026/2027", card.OperationalPeriodName);
        Assert.Equal(StudyGroupId, card.StudyGroupId);
        Assert.Equal("7A", card.StudyGroupName);
        Assert.Equal(HomeroomTeacherId, card.HomeroomTeacherId);
        Assert.Equal("Budi Santoso", card.HomeroomTeacherName);
        Assert.Equal(ReportCardStatus.Draft, card.Status);
        Assert.Null(card.OverrideReason);
        Assert.Null(card.GeneralRemarks);
        Assert.Null(card.FinalizedAt);
        Assert.Equal(FixedNow, card.CreatedAt);
        Assert.Equal(FixedNow, card.UpdatedAt);
        Assert.Empty(card.DomainEvents);
        Assert.Empty(card.Entries);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDraft_WhenStudentNameEmpty_FailsWithValidationError(string? invalidName)
    {
        var result = ReportCard.CreateDraft(
            StudentId,
            invalidName!,
            "1001",
            null,
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester Ganjil",
            StudyGroupId,
            "7A",
            null,
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.StudentIdRequired", result.Error.Code);
    }

    [Fact]
    public void CreateDraft_WhenAcademicYearMissing_FailsWithValidationError()
    {
        var result = ReportCard.CreateDraft(
            StudentId,
            "Ahmad Dahlan",
            "1001",
            null,
            Guid.Empty,
            "",
            OperationalPeriodId,
            "Semester Ganjil",
            null,
            null,
            null,
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.AcademicYearRequired", result.Error.Code);
    }

    [Fact]
    public void CreateDraft_WhenOperationalPeriodMissing_FailsWithValidationError()
    {
        var result = ReportCard.CreateDraft(
            StudentId,
            "Ahmad Dahlan",
            "1001",
            null,
            AcademicYearId,
            "2026/2027",
            Guid.Empty,
            "",
            null,
            null,
            null,
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.OperationalPeriodRequired", result.Error.Code);
    }

    [Fact]
    public void Finalize_WhenStatusIsDraft_TransitionsToFinalizedAndRaisesDomainEvent()
    {
        var card = CreateValidDraftCard();

        var result = card.Finalize(FinalizeNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportCardStatus.Finalized, card.Status);
        Assert.Equal(FinalizeNow, card.FinalizedAt);
        Assert.Equal(FinalizeNow, card.UpdatedAt);

        var domainEvent = Assert.Single(card.DomainEvents);
        var finalizedEvent = Assert.IsType<ReportCardFinalizedDomainEvent>(domainEvent);
        Assert.Equal(card.Id, finalizedEvent.ReportCardId);
        Assert.Equal(card.StudentId, finalizedEvent.StudentId);
        Assert.Equal(card.OperationalPeriodId, finalizedEvent.OperationalPeriodId);
        Assert.Equal(FinalizeNow, finalizedEvent.FinalizedAt);
    }

    [Fact]
    public void Finalize_WhenAlreadyFinalized_FailsWithConflictError()
    {
        var card = CreateValidDraftCard();
        card.Finalize(FinalizeNow);

        var result = card.Finalize(FinalizeNow.AddMinutes(5));

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.AlreadyFinalized", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void OverrideToDraft_WhenStatusIsFinalizedAndReasonProvided_TransitionsToDraft()
    {
        var card = CreateValidDraftCard();
        card.Finalize(FinalizeNow);
        card.ClearDomainEvents();

        var result = card.OverrideToDraft("Perbaikan nilai susulan Matematika", OverrideNow);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReportCardStatus.Draft, card.Status);
        Assert.Null(card.FinalizedAt);
        Assert.Equal("Perbaikan nilai susulan Matematika", card.OverrideReason);
        Assert.Equal(OverrideNow, card.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void OverrideToDraft_WhenReasonEmpty_FailsWithValidationError(string? invalidReason)
    {
        var card = CreateValidDraftCard();
        card.Finalize(FinalizeNow);

        var result = card.OverrideToDraft(invalidReason!, OverrideNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.EmptyOverrideReason", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(ReportCardStatus.Finalized, card.Status);
    }

    [Fact]
    public void OverrideToDraft_WhenCardIsNotFinalized_FailsWithConflictError()
    {
        var card = CreateValidDraftCard();

        var result = card.OverrideToDraft("Perbaikan nilai", OverrideNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.NotFinalized", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public void UpdateHomeroomRemarks_WhenInDraft_UpdatesRemarks()
    {
        var card = CreateValidDraftCard();

        var result = card.UpdateHomeroomRemarks(" Siswa berprestasi sangat baik dan aktif. ", FixedNow);

        Assert.True(result.IsSuccess);
        Assert.Equal("Siswa berprestasi sangat baik dan aktif.", card.GeneralRemarks);
        Assert.Equal(FixedNow, card.UpdatedAt);
    }

    [Fact]
    public void UpdateHomeroomRemarks_WhenFinalized_FailsWithConflictError()
    {
        var card = CreateValidDraftCard();
        card.Finalize(FinalizeNow);

        var result = card.UpdateHomeroomRemarks("Catatan baru", OverrideNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.AlreadyFinalized", result.Error.Code);
    }

    [Fact]
    public void Entry_Create_WhenValid_SucceedsWithTrimmedProperties()
    {
        var cardId = Guid.NewGuid();
        var result = ReportCardEntry.Create(
            cardId,
            SubjectId,
            " Matematika ",
            " MTK ",
            SubjectStageId,
            " Matematika Tingkat 7 ",
            88.50m,
            " A ",
            TeacherId,
            " Budi Santoso ",
            " Catatan awal ",
            utcNow: FixedNow);

        Assert.True(result.IsSuccess);
        var entry = result.Value!;
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(cardId, entry.ReportCardId);
        Assert.Equal(SubjectId, entry.SubjectId);
        Assert.Equal("Matematika", entry.SubjectName);
        Assert.Equal("MTK", entry.SubjectCode);
        Assert.Equal(SubjectStageId, entry.SubjectStageId);
        Assert.Equal("Matematika Tingkat 7", entry.SubjectStageName);
        Assert.Equal(88.50m, entry.NumericScore);
        Assert.Equal("A", entry.LetterGrade);
        Assert.Equal(TeacherId, entry.TeacherId);
        Assert.Equal("Budi Santoso", entry.TeacherName);
        Assert.Equal("Catatan awal", entry.TeacherNotes);
        Assert.Equal(FixedNow, entry.CreatedAt);
        Assert.Equal(FixedNow, entry.UpdatedAt);
    }

    [Fact]
    public void Entry_Create_WhenSubjectStageMissing_FailsWithValidationError()
    {
        var result = ReportCardEntry.Create(
            Guid.NewGuid(),
            SubjectId,
            "Matematika",
            "MTK",
            Guid.Empty,
            "",
            88.50m);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.SubjectStageRequired", result.Error.Code);
    }

    [Fact]
    public void Entry_UpdateTeacherNotes_WhenValid_UpdatesTeacherSnapshotAndNotes()
    {
        var entry = ReportCardEntry.Create(
            Guid.NewGuid(),
            SubjectId,
            "Matematika",
            "MTK",
            SubjectStageId,
            "Matematika Tingkat 7",
            85.00m,
            utcNow: FixedNow).Value!;

        var updateTime = FixedNow.AddHours(2);
        var result = entry.UpdateTeacherNotes(TeacherId, " Budi Santoso ", " Siswa sangat tekun ", updateTime);

        Assert.True(result.IsSuccess);
        Assert.Equal(TeacherId, entry.TeacherId);
        Assert.Equal("Budi Santoso", entry.TeacherName);
        Assert.Equal("Siswa sangat tekun", entry.TeacherNotes);
        Assert.Equal(updateTime, entry.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Entry_UpdateTeacherNotes_WhenEmptyNotes_FailsWithValidationError(string? invalidNotes)
    {
        var entry = ReportCardEntry.Create(
            Guid.NewGuid(),
            SubjectId,
            "Matematika",
            "MTK",
            SubjectStageId,
            "Matematika Tingkat 7",
            85.00m).Value!;

        var result = entry.UpdateTeacherNotes(TeacherId, "Budi Santoso", invalidNotes!, FixedNow);

        Assert.False(result.IsSuccess);
        Assert.Equal("Acd.ReportCards.EmptyTeacherNotes", result.Error.Code);
    }

    [Fact]
    public void Rehydrate_ReportCardAndEntries_RestoresAllStateAccurately()
    {
        var cardId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        var entry = ReportCardEntry.Rehydrate(
            entryId,
            cardId,
            SubjectId,
            "Matematika",
            "MTK",
            SubjectStageId,
            "Matematika Tingkat 7",
            TeacherId,
            "Budi Santoso",
            92.00m,
            "A",
            "Sangat memuaskan",
            FixedNow,
            FixedNow);

        var card = ReportCard.Rehydrate(
            cardId,
            StudentId,
            "Ahmad Dahlan",
            "1001",
            "0081234561",
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester Ganjil 2026/2027",
            StudyGroupId,
            "7A",
            HomeroomTeacherId,
            "Budi Santoso",
            ReportCardStatus.Finalized,
            null,
            "Catatan wali kelas",
            FinalizeNow,
            FixedNow,
            FinalizeNow,
            new[] { entry });

        Assert.Equal(cardId, card.Id);
        Assert.Equal(ReportCardStatus.Finalized, card.Status);
        Assert.Equal("Catatan wali kelas", card.GeneralRemarks);
        Assert.Equal(FinalizeNow, card.FinalizedAt);
        Assert.Single(card.Entries);
        var loadedEntry = card.Entries.First();
        Assert.Equal(entryId, loadedEntry.Id);
        Assert.Equal(92.00m, loadedEntry.NumericScore);
        Assert.Equal("A", loadedEntry.LetterGrade);
    }

    [Theory]
    [InlineData(new[] { 85.0, 90.0, 95.0 }, 90.00)]
    [InlineData(new[] { 80.0, 85.0 }, 82.50)]
    [InlineData(new[] { 70.0, 75.0, 78.0 }, 74.33)]
    [InlineData(new double[] { }, 0.00)]
    public void ComputeArithmeticFinalGrade_CalculatesTwoDecimalRoundedAverage(
        double[] rawScores,
        decimal expectedAverage)
    {
        // Enforces specification: ROUND(AVG(score), 2) or 0.00 if empty
        var average = rawScores.Length == 0
            ? 0.00m
            : Math.Round((decimal)rawScores.Average(), 2, MidpointRounding.AwayFromZero);

        Assert.Equal(expectedAverage, average);

        // Verify creating a report card entry with this computed arithmetic average succeeds
        var entryResult = ReportCardEntry.Create(
            Guid.NewGuid(),
            SubjectId,
            "Matematika",
            "MTK",
            SubjectStageId,
            "Matematika Tingkat 7",
            average,
            letterGrade: average >= 85 ? "A" : average >= 75 ? "B" : "C",
            utcNow: FixedNow);

        Assert.True(entryResult.IsSuccess);
        Assert.Equal(expectedAverage, entryResult.Value!.NumericScore);
    }

    private static ReportCard CreateValidDraftCard()
    {
        return ReportCard.CreateDraft(
            StudentId,
            "Ahmad Dahlan",
            "1001",
            "0081234561",
            AcademicYearId,
            "2026/2027",
            OperationalPeriodId,
            "Semester Ganjil 2026/2027",
            StudyGroupId,
            "7A",
            HomeroomTeacherId,
            "Budi Santoso",
            utcNow: FixedNow).Value!;
    }
}
