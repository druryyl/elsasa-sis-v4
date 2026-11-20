namespace ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;

public sealed record EnterStudentScoresResponse(
    Guid AssignmentId,
    int ScoresUpdatedCount);
