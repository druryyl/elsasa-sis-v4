using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public interface IAnnouncementRepository
{
    Task<Announcement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Announcement announcement, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default);
}
