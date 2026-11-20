using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Events;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.FinalizeHomeroomReportCards;

public sealed class FinalizeHomeroomReportCardsHandler(
    IReportCardRepository reportCardRepository,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentUser currentUser)
{
    public async Task<Result> HandleAsync(
        Guid reportCardId,
        CancellationToken cancellationToken = default)
    {
        var card = await reportCardRepository.GetByIdAsync(reportCardId, cancellationToken);
        if (card is null)
        {
            return Result.Failure(AcdErrors.ReportCards.ReportCardNotFound(reportCardId));
        }

        if (card.Status != ReportCardStatus.Draft)
        {
            return Result.Failure(AcdErrors.ReportCards.AlreadyFinalized(card.Id));
        }

        var isAdmin = currentUser.Roles.Contains("SCHOOL_ADMIN");
        if (!isAdmin && card.HomeroomTeacherId.HasValue)
        {
            if (!currentUser.TeacherId.HasValue || currentUser.TeacherId.Value != card.HomeroomTeacherId.Value)
            {
                return Result.Failure(AcdErrors.ReportCards.NotAssignedHomeroomTeacher(
                    currentUser.TeacherId ?? Guid.Empty,
                    card.StudyGroupId ?? Guid.Empty));
            }
        }

        var finalizeResult = card.Finalize(DateTime.UtcNow);
        if (!finalizeResult.IsSuccess)
        {
            return finalizeResult;
        }

        await reportCardRepository.UpdateAsync(card, cancellationToken);

        if (card.DomainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(card.DomainEvents, cancellationToken);
            card.ClearDomainEvents();
        }

        return Result.Success();
    }
}
