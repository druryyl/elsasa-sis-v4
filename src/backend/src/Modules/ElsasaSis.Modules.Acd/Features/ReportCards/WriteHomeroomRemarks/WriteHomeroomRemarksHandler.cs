using ElsasaSis.Modules.Acd.Domain;
using ElsasaSis.Modules.Acd.Domain.ReportCards;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Security;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.WriteHomeroomRemarks;

public sealed class WriteHomeroomRemarksHandler(
    IReportCardRepository reportCardRepository,
    ICurrentUser currentUser)
{
    public async Task<Result> HandleAsync(
        Guid reportCardId,
        WriteHomeroomRemarksRequest request,
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

        var updateResult = card.UpdateHomeroomRemarks(request.Remarks, DateTime.UtcNow);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        await reportCardRepository.UpdateAsync(card, cancellationToken);

        return Result.Success();
    }
}
