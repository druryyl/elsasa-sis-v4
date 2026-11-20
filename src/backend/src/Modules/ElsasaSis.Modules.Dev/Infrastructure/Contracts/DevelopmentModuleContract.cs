using ElsasaSis.Contracts.Development;

namespace ElsasaSis.Modules.Dev.Infrastructure.Contracts;

public sealed class DevelopmentModuleContract : IDevelopmentModule
{
    public Task<bool> HasActiveDisciplineRecordAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
