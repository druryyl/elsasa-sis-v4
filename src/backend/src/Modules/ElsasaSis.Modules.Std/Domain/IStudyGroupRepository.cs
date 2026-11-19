using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public interface IStudyGroupRepository
{
    Task<StudyGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByYearAndNameAsync(Guid academicYearId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(StudyGroup studyGroup, CancellationToken cancellationToken = default);
}
