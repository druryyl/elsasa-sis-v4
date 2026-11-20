using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Dev.Domain;

public sealed class ExtracurricularParticipation : Entity<Guid>
{
    public Guid ActivityId { get; private set; }
    public string ActivityName { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateOnly JoinedDate { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? WithdrawnAt { get; private set; }
    public string? LastSessionLog { get; private set; }
    public DateOnly? LastSessionDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ExtracurricularParticipation() { }

    private ExtracurricularParticipation(
        Guid id,
        Guid activityId,
        string activityName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid academicYearId,
        string academicYearName,
        string status,
        DateOnly joinedDate,
        DateTime? completedAt,
        DateTime? withdrawnAt,
        string? lastSessionLog,
        DateOnly? lastSessionDate,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        ActivityId = activityId;
        ActivityName = activityName;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        Status = status;
        JoinedDate = joinedDate;
        CompletedAt = completedAt;
        WithdrawnAt = withdrawnAt;
        LastSessionLog = lastSessionLog;
        LastSessionDate = lastSessionDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static ExtracurricularParticipation Create(
        Guid activityId,
        string activityName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid academicYearId,
        string academicYearName,
        DateOnly joinedDate,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            activityId,
            activityName,
            studentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            ExtracurricularParticipationStatus.Active,
            joinedDate,
            null,
            null,
            null,
            null,
            utcNow,
            utcNow);

    internal void Withdraw(DateTime utcNow)
    {
        Status = ExtracurricularParticipationStatus.Withdrawn;
        WithdrawnAt = utcNow;
        UpdatedAt = utcNow;
    }

    internal void Complete(DateTime utcNow)
    {
        Status = ExtracurricularParticipationStatus.Completed;
        CompletedAt = utcNow;
        UpdatedAt = utcNow;
    }

    internal void RecordSessionLog(DateOnly sessionDate, string log, DateTime utcNow)
    {
        LastSessionDate = sessionDate;
        LastSessionLog = log;
        UpdatedAt = utcNow;
    }

    public static ExtracurricularParticipation Rehydrate(
        Guid id,
        Guid activityId,
        string activityName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid academicYearId,
        string academicYearName,
        string status,
        DateOnly joinedDate,
        DateTime? completedAt,
        DateTime? withdrawnAt,
        string? lastSessionLog,
        DateOnly? lastSessionDate,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            activityId,
            activityName,
            studentId,
            studentName,
            studentNis,
            academicYearId,
            academicYearName,
            status,
            joinedDate,
            completedAt,
            withdrawnAt,
            lastSessionLog,
            lastSessionDate,
            createdAt,
            updatedAt);
}
