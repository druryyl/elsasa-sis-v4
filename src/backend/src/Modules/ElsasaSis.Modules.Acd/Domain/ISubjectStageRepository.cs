using ElsasaSis.Modules.Acd.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain;

public interface ISubjectStageRepository
{
    Task<SubjectStage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid subjectId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySequenceOrderAsync(Guid subjectId, int sequenceOrder, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(SubjectStage stage, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(SubjectStage stage, CancellationToken cancellationToken = default);
}
