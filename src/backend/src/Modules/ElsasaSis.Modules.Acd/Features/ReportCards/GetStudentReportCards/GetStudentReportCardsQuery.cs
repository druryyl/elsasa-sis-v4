using ElsasaSis.SharedKernel.Common;
using MediatR;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;

public sealed record GetStudentReportCardsQuery(Guid StudentId)
    : IRequest<Result<IReadOnlyList<StudentReportCardSummaryDto>>>;
