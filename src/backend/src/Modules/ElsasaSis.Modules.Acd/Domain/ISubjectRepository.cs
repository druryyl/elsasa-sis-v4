using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public interface ISubjectRepository
{
    Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Subject subject, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Subject subject, CancellationToken cancellationToken = default);
}
