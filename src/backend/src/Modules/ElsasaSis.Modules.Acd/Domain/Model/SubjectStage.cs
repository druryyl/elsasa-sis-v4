using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public sealed class SubjectStage : AggregateRoot<Guid>
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private readonly List<LearningUnit> _learningUnits = [];
    private readonly List<SubjectStageGrade> _gradeMappings = [];

    public Guid SubjectId { get; private set; }
    public string SubjectName { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SequenceOrder { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<LearningUnit> LearningUnits => _learningUnits.AsReadOnly();
    public IReadOnlyList<SubjectStageGrade> GradeMappings => _gradeMappings.AsReadOnly();

    private SubjectStage()
    {
    }

    private SubjectStage(
        Guid id,
        Guid subjectId,
        string subjectName,
        string name,
        int sequenceOrder,
        string? description,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<LearningUnit>? units = null,
        IEnumerable<SubjectStageGrade>? gradeMappings = null)
        : base(id)
    {
        SubjectId = subjectId;
        SubjectName = subjectName;
        Name = name;
        SequenceOrder = sequenceOrder;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;

        if (units is not null)
        {
            _learningUnits.AddRange(units);
        }

        if (gradeMappings is not null)
        {
            _gradeMappings.AddRange(gradeMappings);
        }
    }

    public static Result<SubjectStage> Create(
        Guid subjectId,
        string subjectName,
        string name,
        int sequenceOrder,
        string? description,
        DateTime? utcNow = null)
    {
        if (subjectId == Guid.Empty)
        {
            return Result<SubjectStage>.Failure(LearningStructureErrors.SubjectNotFound(subjectId));
        }

        if (string.IsNullOrWhiteSpace(subjectName))
        {
            return Result<SubjectStage>.Failure(LearningStructureErrors.EmptySubjectName());
        }

        var validation = Validate(name, sequenceOrder);
        if (validation.IsFailure)
        {
            return Result<SubjectStage>.Failure(validation.Error);
        }

        var now = utcNow ?? DateTime.UtcNow;
        var stage = new SubjectStage(
            UuidV7.NewGuid(),
            subjectId,
            subjectName.Trim(),
            name.Trim(),
            sequenceOrder,
            NormalizeDescription(description),
            now,
            now);

        return Result<SubjectStage>.Success(stage);
    }

    public Result Update(
        string name,
        int sequenceOrder,
        string? description,
        DateTime? utcNow = null)
    {
        var validation = Validate(name, sequenceOrder);
        if (validation.IsFailure)
        {
            return validation;
        }

        Name = name.Trim();
        SequenceOrder = sequenceOrder;
        Description = NormalizeDescription(description);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result<LearningUnit> AddLearningUnit(
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime? utcNow = null)
    {
        var trimmedName = name?.Trim() ?? string.Empty;

        if (_learningUnits.Any(u => u.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<LearningUnit>.Failure(LearningStructureErrors.DuplicateUnitName(trimmedName));
        }

        if (_learningUnits.Any(u => u.SequenceOrder == sequenceOrder))
        {
            return Result<LearningUnit>.Failure(LearningStructureErrors.DuplicateUnitSequence(sequenceOrder));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var unitResult = LearningUnit.Create(Id, trimmedName, sequenceOrder, unitType, description, now);
        if (unitResult.IsFailure)
        {
            return unitResult;
        }

        var unit = unitResult.Value!;
        _learningUnits.Add(unit);
        UpdatedAt = now;

        return Result<LearningUnit>.Success(unit);
    }

    public Result UpdateLearningUnit(
        Guid unitId,
        string name,
        int sequenceOrder,
        string unitType,
        string? description,
        DateTime? utcNow = null)
    {
        var unit = _learningUnits.FirstOrDefault(u => u.Id == unitId);
        if (unit is null)
        {
            return Result.Failure(LearningStructureErrors.LearningUnitNotFound(unitId));
        }

        var trimmedName = name?.Trim() ?? string.Empty;

        if (_learningUnits.Any(u => u.Id != unitId && u.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitName(trimmedName));
        }

        if (_learningUnits.Any(u => u.Id != unitId && u.SequenceOrder == sequenceOrder))
        {
            return Result.Failure(LearningStructureErrors.DuplicateUnitSequence(sequenceOrder));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var updateResult = unit.Update(trimmedName, sequenceOrder, unitType, description, now);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        UpdatedAt = now;
        return Result.Success();
    }

    public Result RemoveLearningUnit(Guid unitId, DateTime? utcNow = null)
    {
        var unit = _learningUnits.FirstOrDefault(u => u.Id == unitId);
        if (unit is null)
        {
            return Result.Failure(LearningStructureErrors.LearningUnitNotFound(unitId));
        }

        _learningUnits.Remove(unit);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result ReorderUnits(IReadOnlyDictionary<Guid, int> unitOrders, DateTime? utcNow = null)
    {
        if (unitOrders.Count != _learningUnits.Count ||
            unitOrders.Keys.Any(id => _learningUnits.All(u => u.Id != id)))
        {
            return Result.Failure(LearningStructureErrors.InvalidReorderUnits());
        }

        var newSequences = unitOrders.Values.ToList();
        if (newSequences.Any(seq => seq <= 0) || newSequences.Distinct().Count() != newSequences.Count)
        {
            return Result.Failure(LearningStructureErrors.InvalidReorderUnits());
        }

        var now = utcNow ?? DateTime.UtcNow;
        foreach (var (unitId, newOrder) in unitOrders)
        {
            var unit = _learningUnits.First(u => u.Id == unitId);
            unit.SetSequenceOrder(newOrder, now);
        }

        UpdatedAt = now;
        return Result.Success();
    }

    public Result<SubjectStageGrade> AddGradeMapping(
        string schoolLevel,
        string grade,
        DateTime? utcNow = null)
    {
        if (!Model.SchoolLevel.IsValidCombination(schoolLevel, grade))
        {
            return Result<SubjectStageGrade>.Failure(LearningStructureErrors.InvalidGradeLevel(schoolLevel, grade));
        }

        var normalizedLevel = Model.SchoolLevel.Normalize(schoolLevel);
        var trimmedGrade = grade.Trim();

        if (_gradeMappings.Any(m =>
            m.SchoolLevel.Equals(normalizedLevel, StringComparison.OrdinalIgnoreCase) &&
            m.Grade.Equals(trimmedGrade, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<SubjectStageGrade>.Failure(LearningStructureErrors.DuplicateGradeMapping(normalizedLevel, trimmedGrade));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var mappingResult = SubjectStageGrade.Create(Id, normalizedLevel, trimmedGrade, now);
        if (mappingResult.IsFailure)
        {
            return mappingResult;
        }

        var mapping = mappingResult.Value!;
        _gradeMappings.Add(mapping);
        UpdatedAt = now;

        return Result<SubjectStageGrade>.Success(mapping);
    }

    public Result RemoveGradeMapping(Guid mappingId, DateTime? utcNow = null)
    {
        var mapping = _gradeMappings.FirstOrDefault(m => m.Id == mappingId);
        if (mapping is null)
        {
            return Result.Failure(LearningStructureErrors.GradeMappingNotFound(mappingId));
        }

        _gradeMappings.Remove(mapping);
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public static SubjectStage Rehydrate(
        Guid id,
        Guid subjectId,
        string subjectName,
        string name,
        int sequenceOrder,
        string? description,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<LearningUnit>? units = null,
        IEnumerable<SubjectStageGrade>? gradeMappings = null)
    {
        return new SubjectStage(
            id,
            subjectId,
            subjectName,
            name,
            sequenceOrder,
            description,
            createdAt,
            updatedAt,
            units,
            gradeMappings);
    }

    private static Result Validate(string name, int sequenceOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(LearningStructureErrors.EmptyStageName());
        }

        if (name.Trim().Length > MaxNameLength)
        {
            return Result.Failure(LearningStructureErrors.StageNameTooLong());
        }

        if (sequenceOrder <= 0)
        {
            return Result.Failure(LearningStructureErrors.InvalidStageSequence());
        }

        return Result.Success();
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
