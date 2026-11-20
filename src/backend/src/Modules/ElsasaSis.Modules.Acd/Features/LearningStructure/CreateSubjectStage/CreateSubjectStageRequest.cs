namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateSubjectStage;

public sealed record CreateSubjectStageRequest(
    string Name,
    int SequenceOrder,
    string? Description);
