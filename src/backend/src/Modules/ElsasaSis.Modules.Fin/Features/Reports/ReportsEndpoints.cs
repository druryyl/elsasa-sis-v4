using ElsasaSis.Modules.Fin.Features.Reports.DailyCashier;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesAging;
using ElsasaSis.Modules.Fin.Features.Reports.ReceivablesSummary;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Fin.Features.Reports;

public sealed class ReportsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var reportsGroup = app.MapGroup("/api/fin/reports")
            .WithTags("Finance - Reports");

        reportsGroup.MapGet("/receivables-summary", async (
                Guid? academicYearId,
                GetReceivablesSummaryHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new GetReceivablesSummaryQuery(academicYearId), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Reports.View)
            .Produces<ReceivablesSummaryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        reportsGroup.MapGet("/receivables-aging", async (
                Guid? academicYearId,
                string? schoolLevel,
                string? grade,
                Guid? studyGroupId,
                string? asOfDate,
                GetReceivablesAgingHandler handler,
                CancellationToken ct) =>
            {
                DateOnly? parsedAsOfDate = null;
                if (!string.IsNullOrWhiteSpace(asOfDate))
                {
                    if (!DateOnly.TryParse(asOfDate, out var d))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Bad Request",
                            detail: "Invalid asOfDate format. Expected YYYY-MM-DD.");
                    }

                    parsedAsOfDate = d;
                }

                var query = new GetReceivablesAgingQuery(
                    academicYearId,
                    schoolLevel,
                    grade,
                    studyGroupId,
                    parsedAsOfDate);

                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Reports.View)
            .Produces<ReceivablesAgingDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        reportsGroup.MapGet("/daily-cashier", async (
                string? date,
                GetDailyCashierSummaryHandler handler,
                CancellationToken ct) =>
            {
                DateOnly? parsedDate = null;
                if (!string.IsNullOrWhiteSpace(date))
                {
                    if (!DateOnly.TryParse(date, out var d))
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status400BadRequest,
                            title: "Bad Request",
                            detail: "Invalid date format. Expected YYYY-MM-DD.");
                    }

                    parsedDate = d;
                }

                var result = await handler.HandleAsync(new GetDailyCashierSummaryQuery(parsedDate), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Reports.View)
            .Produces<DailyCashierSummaryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
