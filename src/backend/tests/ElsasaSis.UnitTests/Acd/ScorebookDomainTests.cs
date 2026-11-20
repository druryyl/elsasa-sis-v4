using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.UnitTests.Acd;

public sealed class ScorebookDomainTests
{
    private static readonly DateTime FixedNow = new(2026, 11, 19, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly ValidAssignedDate = new(2026, 11, 15);
    private static readonly DateOnly ValidDueDate = new(2026, 11, 20);
    private static readonly Guid SubjectStageId = Guid.NewGuid();
    private static readonly Guid SubjectId = Guid.NewGuid();
    private static readonly Guid StudyGroupId = Guid.NewGuid();
    private static readonly Guid OperationalPeriodId = Guid.NewGuid();
    private static readonly Guid TeacherId = Guid.NewGuid();
    private static readonly Guid StudentId = Guid.NewGuid();

    [Fact]
    public void Create_WhenValidParameters_SucceedsWithTrimmedSnapshots()
    {
        var result = Assignment.Create(
            SubjectStageId,
            " Mathematics Stage 7 ",
            SubjectId,
            " Matematika ",
            OperationalPeriodId,
            " Semester Ganjil 2026/2027 ",
            StudyGroupId,
            " 7A ",
            " Kuis Aljabar 1 ",
            AssignmentType.Formative,
            100.00m,
            20.00m,
            ValidAssignedDate,
            ValidDueDate,
            TeacherId,
            " Budi Santoso ",
            null,
            null,
            FixedNow);

        Assert.True(result.IsSuccess);
        var assignment = result.Value!;
        Assert.NotEqual(Guid.Empty, assignment.Id);
        Assert.Equal(SubjectStageId, assignment.SubjectStageId);
        Assert.Equal("Mathematics Stage 7", assignment.SubjectStageName);
        Assert.Equal(SubjectId, assignment.SubjectId);
        Assert.Equal("Matematika", assignment.SubjectName);
        Assert.Equal(OperationalPeriodId, assignment.OperationalPeriodId);
        Assert.Equal("Semester Ganjil 2026/2027", assignment.OperationalPeriodName);
        Assert.Equal(StudyGroupId, assignment.StudyGroupId);
        Assert.Equal("7A", assignment.StudyGroupName);
        Assert.Equal("Kuis Aljabar 1", assignment.Title);
        Assert.Equal(AssignmentType.Formative, assignment.AssignmentType);
        Assert.Equal(100.00m, assignment.MaxScore);
        Assert.Equal(20.00m, assignment.Weight);
        Assert.Equal(ValidAssignedDate, assignment.AssignedDate);
        Assert.Equal(ValidDueDate, assignment.DueDate);
        Assert.Equal(TeacherId, assignment.TeacherId);
        Assert.Equal("Budi Santoso", assignment.TeacherName);
        Assert.Equal(FixedNow, assignment.CreatedAt);
        Assert.Equal(FixedNow, assignment.UpdatedAt);
        Assert.Empty(assignment.Scores);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(-0.01)]
    public void Create_WhenMaxScoreNotPositive_FailsWithValidationError(decimal maxScore)
    {
        var result = Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            "Kuis 1",
            AssignmentType.Formative,
            maxScore,
            null,
            ValidAssignedDate,
            null,
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.MaxScoreMustBePositive", result.Error.Code);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(-10)]
    [InlineData(150)]
    public void Create_WhenWeightOutOfRange_FailsWithValidationError(decimal weight)
    {
        var result = Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            "Kuis 1",
            AssignmentType.Formative,
            100.00m,
            weight,
            ValidAssignedDate,
            null,
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.InvalidWeight", result.Error.Code);
    }

    [Fact]
    public void Create_WhenDueDateEarlierThanAssignedDate_FailsWithValidationError()
    {
        var result = Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            "Kuis 1",
            AssignmentType.Formative,
            100.00m,
            10.00m,
            new DateOnly(2026, 11, 20),
            new DateOnly(2026, 11, 15),
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.DueDateBeforeAssignedDate", result.Error.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleEmpty_FailsWithValidationError(string title)
    {
        var result = Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            title,
            AssignmentType.Formative,
            100.00m,
            null,
            ValidAssignedDate,
            null,
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.TitleRequired", result.Error.Code);
    }

    [Fact]
    public void Create_WhenTitleExceeds150Chars_FailsWithValidationError()
    {
        var longTitle = new string('A', 151);
        var result = Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            longTitle,
            AssignmentType.Formative,
            100.00m,
            null,
            ValidAssignedDate,
            null,
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.TitleTooLong", result.Error.Code);
    }

    [Fact]
    public void AddOrUpdateScore_WhenValidScore_AddsScoreChild()
    {
        var assignment = CreateValidAssignment();

        var scoreResult = assignment.AddOrUpdateScore(
            StudentId,
            " Ahmad Dahlan ",
            " 202607001 ",
            85.50m,
            " Great job! ",
            utcNow: FixedNow);

        Assert.True(scoreResult.IsSuccess);
        var score = scoreResult.Value!;
        Assert.Equal(StudentId, score.StudentId);
        Assert.Equal("Ahmad Dahlan", score.StudentName);
        Assert.Equal("202607001", score.StudentNis);
        Assert.Equal(85.50m, score.Score);
        Assert.Equal("Great job!", score.Feedback);
        Assert.Single(assignment.Scores);
    }

    [Fact]
    public void AddOrUpdateScore_WhenUpdatingExistingScore_UpdatesAndKeepsSingleChild()
    {
        var assignment = CreateValidAssignment();

        assignment.AddOrUpdateScore(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            80.00m,
            "Initial feedback",
            utcNow: FixedNow);

        var later = FixedNow.AddHours(2);
        var updateResult = assignment.AddOrUpdateScore(
            StudentId,
            "Ahmad Dahlan Updated",
            "202607001",
            95.00m,
            "Updated feedback",
            utcNow: later);

        Assert.True(updateResult.IsSuccess);
        Assert.Single(assignment.Scores);
        var updated = assignment.Scores.First();
        Assert.Equal(95.00m, updated.Score);
        Assert.Equal("Updated feedback", updated.Feedback);
        Assert.Equal("Ahmad Dahlan Updated", updated.StudentName);
        Assert.Equal(later, assignment.UpdatedAt);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-10)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void AddOrUpdateScore_WhenScoreOutOfRange_FailsWithValidationError(decimal score)
    {
        var assignment = CreateValidAssignment(); // MaxScore is 100.00m

        var result = assignment.AddOrUpdateScore(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            score,
            null,
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.ScoreOutOfRange", result.Error.Code);
    }

    [Fact]
    public void AddOrUpdateScore_WhenFeedbackExceeds1000Chars_FailsWithValidationError()
    {
        var assignment = CreateValidAssignment();
        var longFeedback = new string('F', 1001);

        var result = assignment.AddOrUpdateScore(
            StudentId,
            "Ahmad Dahlan",
            "202607001",
            85.00m,
            longFeedback,
            utcNow: FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.FeedbackTooLong", result.Error.Code);
    }

    [Fact]
    public void UpdateDetails_WhenMaxScoreReducedBelowExistingScore_FailsWithScoreOutOfRange()
    {
        var assignment = CreateValidAssignment(); // MaxScore is 100.00m
        assignment.AddOrUpdateScore(StudentId, "Ahmad Dahlan", "202607001", 90.00m, null, utcNow: FixedNow);

        var result = assignment.UpdateDetails(
            "Updated Title",
            AssignmentType.Summative,
            80.00m, // reduced below 90.00m
            30.00m,
            ValidAssignedDate,
            ValidDueDate,
            null,
            null,
            FixedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.ScoreOutOfRange", result.Error.Code);
    }

    [Theory]
    [InlineData("FORMATIVE", AssignmentType.Formative)]
    [InlineData("formative", AssignmentType.Formative)]
    [InlineData("SUMMATIVE", AssignmentType.Summative)]
    [InlineData("summative", AssignmentType.Summative)]
    [InlineData("EXAM", AssignmentType.Exam)]
    [InlineData("exam", AssignmentType.Exam)]
    public void AssignmentTypeExtensions_ParseAssignmentType_WhenValid_ParsesSuccessfully(string raw, AssignmentType expected)
    {
        var result = AssignmentTypeExtensions.ParseAssignmentType(raw);
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        Assert.Equal(raw.ToUpperInvariant(), expected.ToCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("UNKNOWN")]
    [InlineData("QUIZ")]
    public void AssignmentTypeExtensions_ParseAssignmentType_WhenInvalid_Fails(string? raw)
    {
        var result = AssignmentTypeExtensions.ParseAssignmentType(raw);
        Assert.True(result.IsFailure);
        Assert.Equal("Acd.Scorebook.InvalidAssignmentType", result.Error.Code);
    }

    private static Assignment CreateValidAssignment()
    {
        return Assignment.Create(
            SubjectStageId,
            "Mathematics Stage 7",
            SubjectId,
            "Matematika",
            OperationalPeriodId,
            "Semester 1",
            StudyGroupId,
            "7A",
            "Kuis Aljabar 1",
            AssignmentType.Formative,
            100.00m,
            15.00m,
            ValidAssignedDate,
            ValidDueDate,
            TeacherId,
            "Budi Santoso",
            utcNow: FixedNow).Value!;
    }
}
