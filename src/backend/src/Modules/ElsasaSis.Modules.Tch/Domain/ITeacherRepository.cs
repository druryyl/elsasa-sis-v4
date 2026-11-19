using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Tch.Domain;

public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNipAsync(string nip, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNikAsync(string nik, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<int> CountActiveAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Teacher teacher, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Teacher teacher, CancellationToken cancellationToken = default);
}
