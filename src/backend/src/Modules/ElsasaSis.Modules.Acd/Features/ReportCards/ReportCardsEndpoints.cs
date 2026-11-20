using ElsasaSis.Modules.Acd.Features.ReportCards.FinalizeHomeroomReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetHomeroomReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.GetReportCardById;
using ElsasaSis.Modules.Acd.Features.ReportCards.OverrideReportCardStatus;
using ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;
using ElsasaSis.Modules.Acd.Features.ReportCards.ReviewAllReportCards;
using ElsasaSis.Modules.Acd.Features.ReportCards.WriteHomeroomRemarks;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Acd.Features.ReportCards;

public sealed class ReportCardsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/acd/report-cards")
            .WithTags("Academic - Report Cards");

        // 1. Batch draft report cards generation (UC-ADM-19)
        group.MapPost("/generate-batch", async (
                GenerateDraftReportCardsRequest request,
                GenerateDraftReportCardsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.Generate)
            .AddEndpointFilter<ValidationFilter<GenerateDraftReportCardsRequest>>()
            .Produces<GenerateDraftReportCardsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 2. Subject teacher notes (UC-TCH-04)
        group.MapPost("/{id:guid}/subject-notes", async (
                Guid id,
                ProvideSubjectNotesRequest request,
                ProvideSubjectReportCardNotesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(new { message = "Subject notes updated successfully." })
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.ProvideNotes)
            .AddEndpointFilter<ValidationFilter<ProvideSubjectNotesRequest>>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 3. Homeroom general remarks (UC-HRM-02)
        group.MapPost("/{id:guid}/homeroom-remarks", async (
                Guid id,
                WriteHomeroomRemarksRequest request,
                WriteHomeroomRemarksHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(new { message = "Homeroom remarks updated successfully." })
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.Remarks)
            .AddEndpointFilter<ValidationFilter<WriteHomeroomRemarksRequest>>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 4. Finalize homeroom report card (UC-HRM-03)
        group.MapPost("/{id:guid}/finalize", async (
                Guid id,
                FinalizeHomeroomReportCardsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(new { message = "Report card finalized successfully." })
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.Finalize)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 5. Override report card status (UC-ADM-20)
        group.MapPost("/{id:guid}/override", async (
                Guid id,
                OverrideReportCardStatusRequest request,
                OverrideReportCardStatusHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(new { message = "Report card unlocked to draft successfully." })
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.Override)
            .AddEndpointFilter<ValidationFilter<OverrideReportCardStatusRequest>>()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        // 6. Get homeroom report cards (UC-HRM-01)
        group.MapGet("/homeroom", async (
                [FromQuery] Guid studyGroupId,
                [FromQuery] Guid operationalPeriodId,
                GetHomeroomReportCardsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(studyGroupId, operationalPeriodId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Policies.Academic.ReportCards.ViewOrManage)
            .Produces<IReadOnlyList<HomeroomReportCardDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 7. Review all report cards overview (UC-PRC-02)
        group.MapGet("/overview", async (
                [FromQuery] Guid operationalPeriodId,
                [FromQuery] Guid? studyGroupId,
                GetReportCardsOverviewHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(operationalPeriodId, studyGroupId, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Academic.ReportCards.View)
            .Produces<ReportCardsOverviewDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 8. Get report card by ID composite printable DTO projection
        group.MapGet("/{id:guid}", async (
                Guid id,
                GetReportCardByIdHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Policies.Academic.ReportCards.ViewOrManage)
            .Produces<ReportCardDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
