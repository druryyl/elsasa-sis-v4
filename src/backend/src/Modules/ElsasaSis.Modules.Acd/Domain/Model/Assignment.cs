using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class Assignment : AggregateRoot<Guid>
{
    private readonly List<AssignmentScore> _scores = new();

    public Guid SubjectStageId { get; private set; }
    public string SubjectStageName { get; private set; } = string.Empty;
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public Guid? LearningUnitId { get; private set; }
    public string? LearningUnitName { get; private set; }
    public Guid OperationalPeriodId { get; private set; }
    public string OperationalPeriodName { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public AssignmentType AssignmentType { get; private set; }
    public decimal MaxScore { get; private set; }
    public decimal? Weight { get; private set; }
    public DateOnly AssignedDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public Guid TeacherId { get; private set; }
    public string TeacherName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<AssignmentScore> Scores => _scores.AsReadOnly();

    private Assignment()
    {
    }

    private Assignment(
        Guid id,
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid? learningUnitId,
        string? learningUnitName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid? studyGroupId,
        string? studyGroupName,
        string title,
        AssignmentType assignmentType,
        decimal maxScore,
        decimal? weight,
        DateOnly assignedDate,
        DateOnly? dueDate,
        Guid teacherId,
        string teacherName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        SubjectStageId = subjectStageId;
        SubjectStageName = subjectStageName;
        SubjectId = subjectId;
        SubjectName = subjectName;
        LearningUnitId = learningUnitId;
        LearningUnitName = learningUnitName;
        OperationalPeriodId = operationalPeriodId;
        OperationalPeriodName = operationalPeriodName;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        Title = title;
        AssignmentType = assignmentType;
        MaxScore = maxScore;
        Weight = weight;
        AssignedDate = assignedDate;
        DueDate = dueDate;
        TeacherId = teacherId;
        TeacherName = teacherName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<Assignment> Create(
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid studyGroupId,
        string studyGroupName,
        string title,
        AssignmentType assignmentType,
        decimal maxScore,
        decimal? weight,
        DateOnly assignedDate,
        DateOnly? dueDate,
        Guid teacherId,
        string teacherName,
        Guid? learningUnitId = null,
        string? learningUnitName = null,
        DateTime? utcNow = null)
    {
        if (subjectStageId == Guid.Empty)
        {
            return Result<Assignment>.Failure(ScorebookErrors.SubjectStageIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectStageName))
        {
            return Result<Assignment>.Failure(ScorebookErrors.SnapshotRequired("subjectStageName", "Subject stage name"));
        }

        if (subjectId == Guid.Empty)
        {
            return Result<Assignment>.Failure(ScorebookErrors.SubjectIdRequired());
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result<Assignment>.Failure(ScorebookErrors.SnapshotRequired("subjectName", "Subject name"));
        }

        if (operationalPeriodId == Guid.Empty)
        {
            return Result<Assignment>.Failure(ScorebookErrors.OperationalPeriodIdRequired());
        }

        if (string.IsNullOrWhiteSpace(operationalPeriodName))
        {
            return Result<Assignment>.Failure(ScorebookErrors.SnapshotRequired("operationalPeriodName", "Operational period name"));
        }

        if (studyGroupId == Guid.Empty)
        {
            return Result<Assignment>.Failure(ScorebookErrors.StudyGroupIdRequired());
        }

        if (string.IsNullOrWhiteSpace(studyGroupName))
        {
            return Result<Assignment>.Failure(ScorebookErrors.SnapshotRequired("studyGroupName", "Study group name"));
        }

        if (teacherId == Guid.Empty)
        {
            return Result<Assignment>.Failure(ScorebookErrors.TeacherIdRequired());
        }

        if (string.IsNullOrWhiteSpace(teacherName))
        {
            return Result<Assignment>.Failure(ScorebookErrors.SnapshotRequired("teacherName", "Teacher name"));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<Assignment>.Failure(ScorebookErrors.TitleRequired());
        }

        if (title.Trim().Length > 150)
        {
            return Result<Assignment>.Failure(ScorebookErrors.TitleTooLong(150));
        }

        if (maxScore <= 0.00m)
        {
            return Result<Assignment>.Failure(ScorebookErrors.MaxScoreMustBePositive());
        }

        if (weight.HasValue && (weight.Value < 0.00m || weight.Value > 100.00m))
        {
            return Result<Assignment>.Failure(ScorebookErrors.InvalidWeight(weight.Value));
        }

        if (dueDate.HasValue && dueDate.Value < assignedDate)
        {
            return Result<Assignment>.Failure(ScorebookErrors.DueDateBeforeAssignedDate());
        }

        var now = utcNow ?? DateTime.UtcNow;

        return Result<Assignment>.Success(
            new Assignment(
                UuidV7.NewGuid(),
                subjectStageId,
                subjectStageName.Trim(),
                subjectId,
                subjectName.Trim(),
                learningUnitId,
                string.IsNullOrWhiteSpace(learningUnitName) ? null : learningUnitName.Trim(),
                operationalPeriodId,
                operationalPeriodName.Trim(),
                studyGroupId,
                studyGroupName.Trim(),
                title.Trim(),
                assignmentType,
                maxScore,
                weight,
                assignedDate,
                dueDate,
                teacherId,
                teacherName.Trim(),
                now,
                now));
    }

    public Result UpdateDetails(
        string title,
        AssignmentType assignmentType,
        decimal maxScore,
        decimal? weight,
        DateOnly assignedDate,
        DateOnly? dueDate,
        Guid? learningUnitId,
        string? learningUnitName,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure(ScorebookErrors.TitleRequired());
        }

        if (title.Trim().Length > 150)
        {
            return Result.Failure(ScorebookErrors.TitleTooLong(150));
        }

        if (maxScore <= 0.00m)
        {
            return Result.Failure(ScorebookErrors.MaxScoreMustBePositive());
        }

        if (weight.HasValue && (weight.Value < 0.00m || weight.Value > 100.00m))
        {
            return Result.Failure(ScorebookErrors.InvalidWeight(weight.Value));
        }

        if (dueDate.HasValue && dueDate.Value < assignedDate)
        {
            return Result.Failure(ScorebookErrors.DueDateBeforeAssignedDate());
        }

        // Check if any existing score exceeds the new maxScore
        foreach (var score in _scores)
        {
            if (score.Score > maxScore)
            {
                return Result.Failure(ScorebookErrors.ScoreOutOfRange(score.Score, maxScore, score.StudentId.ToString()));
            }
        }

        var now = utcNow ?? DateTime.UtcNow;
        Title = title.Trim();
        AssignmentType = assignmentType;
        MaxScore = maxScore;
        Weight = weight;
        AssignedDate = assignedDate;
        DueDate = dueDate;
        LearningUnitId = learningUnitId;
        LearningUnitName = string.IsNullOrWhiteSpace(learningUnitName) ? null : learningUnitName.Trim();
        UpdatedAt = now;

        return Result.Success();
    }

    public Result<AssignmentScore> AddOrUpdateScore(
        Guid studentId,
        string studentName,
        string studentNis,
        decimal score,
        string? feedback,
        DateTime? gradedAt = null,
        DateTime? utcNow = null)
    {
        var existingScore = _scores.FirstOrDefault(s => s.StudentId == studentId);
        var now = utcNow ?? DateTime.UtcNow;

        if (existingScore is not null)
        {
            var updateResult = existingScore.Update(studentName, studentNis, score, MaxScore, feedback, gradedAt, now);
            if (updateResult.IsFailure)
            {
                return Result<AssignmentScore>.Failure(updateResult.Error);
            }

            UpdatedAt = now;
            return Result<AssignmentScore>.Success(existingScore);
        }

        var createResult = AssignmentScore.Create(
            Id,
            studentId,
            studentName,
            studentNis,
            score,
            MaxScore,
            feedback,
            gradedAt,
            now);

        if (createResult.IsFailure)
        {
            return Result<AssignmentScore>.Failure(createResult.Error);
        }

        _scores.Add(createResult.Value!);
        UpdatedAt = now;
        return Result<AssignmentScore>.Success(createResult.Value!);
    }

    public static Assignment Rehydrate(
        Guid id,
        Guid subjectStageId,
        string subjectStageName,
        Guid subjectId,
        string subjectName,
        Guid? learningUnitId,
        string? learningUnitName,
        Guid operationalPeriodId,
        string operationalPeriodName,
        Guid? studyGroupId,
        string? studyGroupName,
        string title,
        AssignmentType assignmentType,
        decimal maxScore,
        decimal? weight,
        DateOnly assignedDate,
        DateOnly? dueDate,
        Guid teacherId,
        string teacherName,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<AssignmentScore>? scores = null)
    {
        var assignment = new Assignment(
            id,
            subjectStageId,
            subjectStageName,
            subjectId,
            subjectName,
            learningUnitId,
            learningUnitName,
            operationalPeriodId,
            operationalPeriodName,
            studyGroupId,
            studyGroupName,
            title,
            assignmentType,
            maxScore,
            weight,
            assignedDate,
            dueDate,
            teacherId,
            teacherName,
            createdAt,
            updatedAt);

        if (scores is not null)
        {
            assignment._scores.AddRange(scores);
        }

        return assignment;
    }
}
