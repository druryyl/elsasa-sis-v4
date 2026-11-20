using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class LearningUnit : Entity<Guid>
{
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 500;

    public Guid SubjectStageId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SequenceOrder { get; private set; }
    public string UnitType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private LearningUnit()
    {
    }

    private LearningUnit(
        Guid id,
        Guid subjectStageId,
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        SubjectStageId = subjectStageId;
        Name = name;
        SequenceOrder = sequenceOrder;
        UnitType = unitType;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    internal static Result<LearningUnit> Create(
        Guid subjectStageId,
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime now)
    {
        var validation = Validate(name, sequenceOrder, unitType, description);
        if (validation.IsFailure)
        {
            return Result<LearningUnit>.Failure(validation.Error);
        }

        var unit = new LearningUnit(
            UuidV7.NewGuid(),
            subjectStageId,
            name.Trim(),
            sequenceOrder,
            Model.UnitType.Normalize(unitType),
            NormalizeDescription(description),
            now,
            now);

        return Result<LearningUnit>.Success(unit);
    }

    internal Result Update(
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime now)
    {
        var validation = Validate(name, sequenceOrder, unitType, description);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        SequenceOrder = sequenceOrder;
        UnitType = Model.UnitType.Normalize(unitType);
        Description = NormalizeDescription(description);
        UpdatedAt = now;
        return Result.Success();
    }

    internal void SetSequenceOrder(int newSequenceOrder, DateTime now)
    {
        SequenceOrder = newSequenceOrder;
        UpdatedAt = now;
    }

    public static LearningUnit Rehydrate(
        Guid id,
        Guid subjectStageId,
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new LearningUnit(
            id,
            subjectStageId,
            name,
            sequenceOrder,
            unitType,
            description,
            createdAt,
            updatedAt);
    }

    private static Result Validate(string name, int sequenceOrder, string unitType, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(LearningStructureErrors.EmptyUnitName());
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(LearningStructureErrors.UnitNameTooLong());
        }

        if (sequenceOrder <= 0)
        {
            return Result.Failure(LearningStructureErrors.InvalidUnitSequence());
        }

        if (!Model.UnitType.IsValid(unitType))
        {
            return Result.Failure(LearningStructureErrors.InvalidUnitType(unitType));
        }

        return Result.Success();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
