using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.OverrideReportCardStatus;

public sealed class OverrideReportCardStatusHandler(
    IReportCardRepository reportCardRepository)
{
    public async Task<Result> HandleAsync(
        Guid reportCardId,
        OverrideReportCardStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var card = await reportCardRepository.GetByIdAsync(reportCardId, cancellationToken);
        if (card is null)
        {
            return Result.Failure(AcdErrors.ReportCards.ReportCardNotFound(reportCardId));
        }

        if (card.Status != ReportCardStatus.Finalized)
        {
            return Result.Failure(AcdErrors.ReportCards.NotFinalized(card.Id));
        }

        var overrideResult = card.OverrideToDraft(request.OverrideReason, DateTime.UtcNow);
        if (!overrideResult.IsSuccess)
        {
            return overrideResult;
        }

        await reportCardRepository.UpdateAsync(card, cancellationToken);

        return Result.Success();
    }
}
