using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GetStudentReportCards;

public sealed class GetStudentReportCardsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/acd/report-cards/student/{studentId:guid}", async (
                Guid studentId,
                GetStudentReportCardsHandler handler,
                CancellationToken ct) =>
            {
                var query = new GetStudentReportCardsQuery(studentId);
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .WithTags("Academic - Report Cards")
            .RequireAuthorization(Permissions.Academic.ReportCards.View)
            .Produces<IReadOnlyList<StudentReportCardSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
