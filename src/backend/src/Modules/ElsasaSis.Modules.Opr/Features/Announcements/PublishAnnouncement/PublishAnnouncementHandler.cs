using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.Announcements.PublishAnnouncement;

public sealed class PublishAnnouncementHandler(
    IAnnouncementRepository repository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AnnouncementDto>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var announcement = await repository.GetByIdAsync(id, cancellationToken);
        if (announcement is null)
        {
            return Result<AnnouncementDto>.Failure(OprErrors.AnnouncementNotFound(id));
        }

        var publishResult = announcement.Publish();
        if (publishResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(publishResult.Error);
        }

        var saveResult = await repository.UpdateAsync(announcement, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(saveResult.Error);
        }

        if (announcement.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(announcement.DomainEvents, cancellationToken);
            announcement.ClearDomainEvents();
        }

        var academicYear = await operationalContract.GetAcademicYearByIdAsync(announcement.AcademicYearId, cancellationToken);
        return Result<AnnouncementDto>.Success(announcement.ToDto(academicYear?.Name ?? string.Empty));
    }
}
