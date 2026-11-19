using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Std.Domain;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNisAsync(string nis, Guid? excludeStudentId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNisnAsync(string nisn, Guid? excludeStudentId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Student student, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Student student, CancellationToken cancellationToken = default);
}
