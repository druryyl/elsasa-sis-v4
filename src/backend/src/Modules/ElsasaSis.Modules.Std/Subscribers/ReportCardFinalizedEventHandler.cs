using ElsasaSis.Contracts.Academic.Events;
using ElsasaSis.Modules.Std.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ElsasaSis.Modules.Std.Subscribers;

public sealed class ReportCardFinalizedEventHandler(
    IStudentPlacementRepository placementRepository,
    ILogger<ReportCardFinalizedEventHandler> logger)
    : INotificationHandler<ReportCardFinalizedDomainEvent>
{
    public async Task Handle(ReportCardFinalizedDomainEvent notification, CancellationToken cancellationToken)
    {
        await placementRepository.MarkAcademicallyCompletedAsync(
            notification.StudentId,
            notification.OperationalPeriodId,
            cancellationToken);

        logger.LogInformation(
            "ReportCardFinalized event handled for StudentId={StudentId}, OperationalPeriodId={OperationalPeriodId}.",
            notification.StudentId,
            notification.OperationalPeriodId);
    }
}
