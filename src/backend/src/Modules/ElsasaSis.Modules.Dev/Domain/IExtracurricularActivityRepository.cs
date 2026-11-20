namespace ElsasaSis.Modules.Dev.Domain;

public interface IExtracurricularActivityRepository
{
    Task<ExtracurricularActivity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ExtracurricularActivity?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtracurricularActivity>> GetAllAsync(string? status = null, Guid? supervisorId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExtracurricularParticipation>> GetParticipationsByActivityIdAsync(Guid activityId, Guid? academicYearId = null, CancellationToken cancellationToken = default);
    Task<ExtracurricularParticipation?> GetParticipationByIdAsync(Guid participationId, CancellationToken cancellationToken = default);
    Task InsertAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default);
    Task UpdateAsync(ExtracurricularActivity activity, CancellationToken cancellationToken = default);
    Task InsertParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default);
    Task UpdateParticipationAsync(ExtracurricularParticipation participation, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
