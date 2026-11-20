using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class AssignmentScore : Entity<Guid>
{
    public Guid AssignmentId { get; private set; }
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public decimal Score { get; private set; }
    public string? Feedback { get; private set; }
    public DateTime GradedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private AssignmentScore()
    {
    }

    private AssignmentScore(
        Guid id,
        Guid assignmentId,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal score,
        string? feedback,
        DateTime gradedAt,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        AssignmentId = assignmentId;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        Score = score;
        Feedback = feedback;
        GradedAt = gradedAt;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<AssignmentScore> Create(
        Guid assignmentId,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal score,
        decimal maxScore,
        string? feedback,
        DateTime? gradedAt = null,
        DateTime? utcNow = null)
    {
        if (assignmentId == Guid.Empty)
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.SnapshotRequired("assignmentId", "Assignment identifier"));
        }

        if (studentId == Guid.Empty)
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.SnapshotRequired("studentId", "Student identifier"));
        }

        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.SnapshotRequired("studentName", "Student name"));
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.SnapshotRequired("studentNis", "Student NIS"));
        }

        if (score < 0.00m || score > maxScore)
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.ScoreOutOfRange(score, maxScore, studentId.ToString()));
        }

        if (feedback is not null && feedback.Length > 1000)
        {
            return Result<AssignmentScore>.Failure(ScorebookErrors.FeedbackTooLong());
        }

        var now = utcNow ?? DateTime.UtcNow;
        var graded = gradedAt ?? now;

        return Result<AssignmentScore>.Success(
            new AssignmentScore(
                UuidV7.NewGuid(),
                assignmentId,
                studentId,
                studentName.Trim(),
                studentNis.Trim(),
                score,
                string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim(),
                graded,
                now,
                now));
    }

    public Result Update(
        string studentName,
        string studentNis,
        decimal score,
        decimal maxScore,
        string? feedback,
        DateTime? gradedAt = null,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(studentName))
        {
            return Result.Failure(ScorebookErrors.SnapshotRequired("studentName", "Student name"));
        }

        if (string.IsNullOrWhiteSpace(studentNis))
        {
            return Result.Failure(ScorebookErrors.SnapshotRequired("studentNis", "Student NIS"));
        }

        if (score < 0.00m || score > maxScore)
        {
            return Result.Failure(ScorebookErrors.ScoreOutOfRange(score, maxScore, StudentId.ToString()));
        }

        if (feedback is not null && feedback.Length > 1000)
        {
            return Result.Failure(ScorebookErrors.FeedbackTooLong());
        }

        var now = utcNow ?? DateTime.UtcNow;
        StudentName = studentName.Trim();
        StudentNis = studentNis.Trim();
        Score = score;
        Feedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        GradedAt = gradedAt ?? now;
        UpdatedAt = now;

        return Result.Success();
    }

    public static AssignmentScore Rehydrate(
        Guid id,
        Guid assignmentId,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal score,
        string? feedback,
        DateTime gradedAt,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            assignmentId,
            studentId,
            studentName,
            studentNis,
            score,
            feedback,
            gradedAt,
            createdAt,
            updatedAt);
}
