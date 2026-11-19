namespace ElsasaSis.Modules.Std.Domain;

public interface ICandidateRepository
{
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default);
    Task UpdateAsync(Candidate candidate, CancellationToken cancellationToken = default);
}
