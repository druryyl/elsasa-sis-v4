namespace ElsasaSis.Api.Features.Academic.GetSubjects;

public sealed record SubjectResponse(
    Guid Id,
    string Name,
    string? Code);
