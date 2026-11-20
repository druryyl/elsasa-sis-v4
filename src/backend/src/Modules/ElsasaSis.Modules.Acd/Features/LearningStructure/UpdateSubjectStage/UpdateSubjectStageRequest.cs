namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;

public sealed record UpdateSubjectStageRequest(
    string Name,
    int SequenceOrder,
    string? Description);
