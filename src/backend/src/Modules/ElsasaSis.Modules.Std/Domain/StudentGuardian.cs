using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Std.Domain;

public sealed class StudentGuardian : Entity<Guid>
{
    public Guid StudentId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public GuardianRelationshipType RelationshipType { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private StudentGuardian()
    {
    }

    private StudentGuardian(
        Guid id,
        Guid studentId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        StudentId = studentId;
        FullName = fullName;
        RelationshipType = relationshipType;
        Phone = phone;
        Email = email;
        Address = address;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static StudentGuardian Create(
        Guid studentId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            studentId,
            fullName.Trim(),
            relationshipType,
            StdDomainParsing.NormalizeOptional(phone),
            StdDomainParsing.NormalizeOptional(email),
            StdDomainParsing.NormalizeOptional(address),
            utcNow,
            utcNow);

    internal static StudentGuardian CloneFromCandidateGuardian(
        Guid studentId,
        CandidateGuardian source,
        DateTime utcNow) =>
        new(
            UuidV7.NewGuid(),
            studentId,
            source.FullName.Trim(),
            source.RelationshipType,
            StdDomainParsing.NormalizeOptional(source.Phone),
            StdDomainParsing.NormalizeOptional(source.Email),
            StdDomainParsing.NormalizeOptional(source.Address),
            utcNow,
            utcNow);

    public static StudentGuardian Rehydrate(
        Guid id,
        Guid studentId,
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime createdAt,
        DateTime updatedAt) =>
        new(
            id,
            studentId,
            fullName,
            relationshipType,
            phone,
            email,
            address,
            createdAt,
            updatedAt);

    internal void UpdateDetails(
        string fullName,
        GuardianRelationshipType relationshipType,
        string? phone,
        string? email,
        string? address,
        DateTime utcNow)
    {
        FullName = fullName.Trim();
        RelationshipType = relationshipType;
        Phone = StdDomainParsing.NormalizeOptional(phone);
        Email = StdDomainParsing.NormalizeOptional(email);
        Address = StdDomainParsing.NormalizeOptional(address);
        UpdatedAt = utcNow;
    }
}
