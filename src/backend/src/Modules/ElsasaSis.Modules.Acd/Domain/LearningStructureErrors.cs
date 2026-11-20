using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public static class LearningStructureErrors
{
    public static DomainError SubjectNotFound(Guid id) =>
        DomainError.NotFound("Acd.Subject.NotFound", $"Subject '{id}' was not found.");

    public static DomainError DuplicateSubjectName(string name) =>
        DomainError.Conflict("Acd.Subject.DuplicateName", $"A subject named '{name}' already exists.");

    public static DomainError DuplicateSubjectCode(string code) =>
        DomainError.Conflict("Acd.Subject.DuplicateCode", $"A subject with code '{code}' already exists.");

    public static DomainError EmptySubjectName() =>
        DomainError.Validation("Acd.Subject.EmptyName", "Subject name cannot be empty.", "name");

    public static DomainError SubjectNameTooLong() =>
        DomainError.Validation("Acd.Subject.NameTooLong", "Subject name must not exceed 100 characters.", "name");

    public static DomainError SubjectCodeTooLong() =>
        DomainError.Validation("Acd.Subject.CodeTooLong", "Subject code must not exceed 30 characters.", "code");

    public static DomainError SubjectStageNotFound(Guid id) =>
        DomainError.NotFound("Acd.SubjectStage.NotFound", $"Subject stage '{id}' was not found.");

    public static DomainError DuplicateStageName(string name) =>
        DomainError.Conflict("Acd.SubjectStage.DuplicateName", $"A stage named '{name}' already exists in this subject.");

    public static DomainError DuplicateStageSequence(int sequenceOrder) =>
        DomainError.Conflict("Acd.SubjectStage.DuplicateSequence", $"A stage with sequence order '{sequenceOrder}' already exists in this subject.");

    public static DomainError EmptyStageName() =>
        DomainError.Validation("Acd.SubjectStage.EmptyName", "Subject stage name cannot be empty.", "name");

    public static DomainError StageNameTooLong() =>
        DomainError.Validation("Acd.SubjectStage.NameTooLong", "Subject stage name must not exceed 100 characters.", "name");

    public static DomainError InvalidStageSequence() =>
        DomainError.Validation("Acd.SubjectStage.InvalidSequence", "Sequence order must be greater than zero.", "sequenceOrder");

    public static DomainError LearningUnitNotFound(Guid id) =>
        DomainError.NotFound("Acd.LearningUnit.NotFound", $"Learning unit '{id}' was not found in this subject stage.");

    public static DomainError DuplicateUnitName(string name) =>
        DomainError.Conflict("Acd.LearningUnit.DuplicateName", $"A learning unit named '{name}' already exists in this stage.");

    public static DomainError DuplicateUnitSequence(int sequenceOrder) =>
        DomainError.Conflict("Acd.LearningUnit.DuplicateSequence", $"A learning unit with sequence order '{sequenceOrder}' already exists in this stage.");

    public static DomainError EmptyUnitName() =>
        DomainError.Validation("Acd.LearningUnit.EmptyName", "Learning unit name cannot be empty.", "name");

    public static DomainError UnitNameTooLong() =>
        DomainError.Validation("Acd.LearningUnit.NameTooLong", "Learning unit name must not exceed 150 characters.", "name");

    public static DomainError InvalidUnitSequence() =>
        DomainError.Validation("Acd.LearningUnit.InvalidSequence", "Sequence order must be greater than zero.", "sequenceOrder");

    public static DomainError InvalidUnitType(string unitType) =>
        DomainError.Validation("Acd.LearningUnit.InvalidUnitType", $"Invalid unit type '{unitType}'. Allowed types: MODULE, PACE, LESSON, PRESENTATION, BOOK, OTHER.", "unitType");

    public static DomainError GradeMappingNotFound(Guid id) =>
        DomainError.NotFound("Acd.GradeMapping.NotFound", $"Grade mapping '{id}' was not found in this subject stage.");

    public static DomainError DuplicateGradeMapping(string schoolLevel, string grade) =>
        DomainError.Conflict("Acd.GradeMapping.Duplicate", $"Grade mapping for '{schoolLevel}' Grade '{grade}' already exists in this stage.");

    public static DomainError InvalidGradeLevel(string schoolLevel, string grade) =>
        DomainError.Validation("Acd.GradeMapping.InvalidGradeLevel", $"Invalid grade combination '{grade}' for school level '{schoolLevel}'.", "grade");

    public static DomainError InvalidReorderUnits() =>
        DomainError.Validation("Acd.LearningUnit.InvalidReorder", "Reorder request must contain distinct sequence orders matching all units in the stage.", "unitOrders");
}
