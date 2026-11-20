namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateLearningUnit;

public sealed record UpdateLearningUnitRequest(
    string Name,
    int SequenceOrder,
    string UnitType,
    string? Description);
