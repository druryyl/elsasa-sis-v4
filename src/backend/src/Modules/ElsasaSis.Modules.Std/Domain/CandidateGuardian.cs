using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class CandidateGuardian : Entity<Guid>
{
    public Guid CandidateId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public GuardianRelationshipType RelationshipType { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CandidateGuardian()
    {
    }

    private CandidateGuardian(
        Guid id,
        Guid candidateId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        CandidateId = candidateId;
        FullName = fullName;
        RelationshipType = relationshipType;
        Phone = phone;
        Email = email;
        Address = address;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static CandidateGuardian Create(
        Guid candidateId,
        GuardianInput input,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            candidateId,
            input.FullName.Trim(),
            input.RelationshipType,
            StdDomainParsing.NormalizeOptional(input.Phone),
            StdDomainParsing.NormalizeOptional(input.Email),
            StdDomainParsing.NormalizeOptional(input.Address),
            utcNow,
            utcNow);

    public static CandidateGuardian Rehydrate(
        Guid id,
        Guid candidateId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            candidateId,
            fullName,
            relationshipType,
            phone,
            email,
            address,
            createdAt,
            updatedAt);
}
