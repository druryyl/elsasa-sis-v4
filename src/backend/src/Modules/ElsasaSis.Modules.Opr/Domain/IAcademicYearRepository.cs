namespace ElsasaSis.Modules.Opr.Domain;

public interface IAcademicYearRepository
{
    Task<AcademicYear?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(AcademicYear academicYear, CancellationToken cancellationToken = default);
    Task UpdateAsync(AcademicYear academicYear, CancellationToken cancellationToken = default);
    Task SaveActivationAsync(AcademicYear academicYear, CancellationToken cancellationToken = default);
}
