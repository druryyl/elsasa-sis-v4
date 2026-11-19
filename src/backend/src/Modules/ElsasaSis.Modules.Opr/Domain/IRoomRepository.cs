using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Room room, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Room room, CancellationToken cancellationToken = default);
}
