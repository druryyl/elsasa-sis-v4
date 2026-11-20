namespace ElsasaSis.Contracts.Development;

public interface IDevelopmentModule
{
    Task<bool> HasActiveDisciplineRecordAsync(Guid studentId, CancellationToken cancellationToken = default);
}
