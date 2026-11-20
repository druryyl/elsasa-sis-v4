namespace ElsasaSis.Modules.Dev.Domain;

public interface IAchievementRepository
{
    Task<Achievement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Achievement>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Achievement>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default);
    Task InsertAsync(Achievement achievement, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
