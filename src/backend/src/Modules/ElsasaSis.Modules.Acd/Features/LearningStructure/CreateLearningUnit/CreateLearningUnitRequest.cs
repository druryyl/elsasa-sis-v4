namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;

public sealed record CreateLearningUnitRequest(
    string Name,
    int SequenceOrder,
    string UnitType,
    string? Description);
