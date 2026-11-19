using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Tch.Domain;

public sealed class TeachingCapability : Entity<Guid>
{
    public Guid TeacherId { get; private set; }
    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TeachingCapability()
    {
    }

    private TeachingCapability(
        Guid id,
        Guid teacherId,
        Guid subjectId,
        string subjectName,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        TeacherId = teacherId;
        SubjectId = subjectId;
        SubjectName = subjectName;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static TeachingCapability Create(
        Guid teacherId,
        Guid subjectId,
        string subjectName,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            teacherId,
            subjectId,
            subjectName.Trim(),
            utcNow,
            utcNow);

    public static TeachingCapability Rehydrate(
        Guid id,
        Guid teacherId,
        Guid subjectId,
        string subjectName,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(id, teacherId, subjectId, subjectName, createdAt, updatedAt);
}
