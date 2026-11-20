namespace ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;

public sealed record EnterStudentScoresRequest(
    IReadOnlyList<ScoreEntryItemDto> Items);

public sealed record ScoreEntryItemDto(
    Guid StudentId,
    decimal Score,
    string? Feedback);
