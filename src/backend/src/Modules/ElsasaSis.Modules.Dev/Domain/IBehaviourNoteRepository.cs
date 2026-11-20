namespace ElsasaSis.Modules.Dev.Domain;

public interface IBehaviourNoteRepository
{
    Task<BehaviourNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BehaviourNote>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BehaviourNote>> GetAllAsync(Guid? studentId = null, Guid? academicYearId = null, CancellationToken cancellationToken = default);
    Task InsertAsync(BehaviourNote note, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
