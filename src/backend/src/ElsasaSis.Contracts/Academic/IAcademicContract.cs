namespace ElsasaSis.Contracts.Academic;

public sealed record SubjectDto(
    Guid Id,
    string Name,
    string? Code);

public interface IAcademicContract
{
    Task<IReadOnlyList<SubjectDto>> GetAllSubjectsAsync(CancellationToken cancellationToken = default);
    Task<SubjectDto?> GetSubjectByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
