using ElsasaSis.Contracts.Operational;
using ElsasaSis.Modules.Opr.Domain;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;

public sealed class CreateAnnouncementHandler(
    IAnnouncementRepository repository,
    IOperationalContract operationalContract,
    ICurrentUser currentUser,
    IDomainEventDispatcher domainEventDispatcher)
{
    public async Task<Result<AnnouncementDto>> HandleAsync(
        CreateAnnouncementRequest request,
        CancellationToken cancellationToken = default)
    {
        Guid academicYearId;
        string academicYearName;

        if (request.AcademicYearId.HasValue)
        {
            var year = await operationalContract.GetAcademicYearByIdAsync(request.AcademicYearId.Value, cancellationToken);
            if (year is null)
            {
                return Result<AnnouncementDto>.Failure(OprErrors.NotFound(request.AcademicYearId.Value));
            }

            academicYearId = year.Id;
            academicYearName = year.Name;
        }
        else
        {
            var activeYear = await operationalContract.GetActiveAcademicYearAsync(cancellationToken);
            if (activeYear is null)
            {
                return Result<AnnouncementDto>.Failure(OprErrors.NoActiveAcademicYear());
            }

            academicYearId = activeYear.Id;
            academicYearName = activeYear.Name;
        }

        var audienceResult = AnnouncementAudienceExtensions.Parse(request.TargetAudience);
        if (audienceResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(audienceResult.Error);
        }

        var authorName = string.IsNullOrWhiteSpace(currentUser.FullName)
            ? currentUser.Username
            : currentUser.FullName;

        var createResult = Announcement.Create(
            academicYearId,
            request.Title,
            request.Content,
            currentUser.UserId,
            authorName,
            audienceResult.Value);

        if (createResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(createResult.Error);
        }

        var announcement = createResult.Value!;

        var saveResult = await repository.AddAsync(announcement, cancellationToken);
        if (saveResult.IsFailure)
        {
            return Result<AnnouncementDto>.Failure(saveResult.Error);
        }

        if (announcement.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(announcement.DomainEvents, cancellationToken);
            announcement.ClearDomainEvents();
        }

        return Result<AnnouncementDto>.Success(announcement.ToDto(academicYearName));
    }
}
