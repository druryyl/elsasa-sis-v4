using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;

namespace ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;

public sealed class UpdateAnnouncementHandler(
    IAnnouncementRepository repository,
    IOperationalContract operationalContract,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AnnouncementDto>> HandleAsync(
        Guid id,
        UpdateAnnouncementRequest request,
        CancellationToken cancellationToken = default)
    {
        var announcement = await repository.GetByIdAsync(id, cancellationToken);
        if (announcement is null)
        {
            return Result<AnnouncementDto>.Failure(OprErrors.AnnouncementNotFound(id));
        }

        var audienceResult = AnnouncementAudienceExtensions.Parse(request.TargetAudience);
        if (audienceResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(audienceResult.Error);
        }

        var updateResult = announcement.Update(
            request.Title,
            request.Content,
            audienceResult.Value);

        if (updateResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(updateResult.Error);
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
