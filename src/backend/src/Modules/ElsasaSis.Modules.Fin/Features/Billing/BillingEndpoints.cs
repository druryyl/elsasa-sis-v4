using ElsasaSis.Modules.Fin.Features.Billing.CancelStudentCharge;
using ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;
using ElsasaSis.Modules.Fin.Features.Billing.GenerateBatchTuitionCharges;
using ElsasaSis.Modules.Fin.Features.Billing.GetBatchBillingPreview;
using ElsasaSis.Modules.Fin.Features.Billing.GetStudentCharges;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Fin.Features.Billing;

public sealed class BillingEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var chargesGroup = app.MapGroup("/api/fin/charges")
            .WithTags("Finance - Billing");

        chargesGroup.MapPost("/batch-preview", async (
                BatchBillingPreviewRequest request,
                GetBatchBillingPreviewHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Billing.Generate)
            .AddEndpointFilter<ValidationFilter<BatchBillingPreviewRequest>>()
            .Produces<BatchBillingPreviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargesGroup.MapPost("/generate-batch", async (
                GenerateBatchTuitionChargesRequest request,
                GenerateBatchTuitionChargesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Billing.Generate)
            .AddEndpointFilter<ValidationFilter<GenerateBatchTuitionChargesRequest>>()
            .Produces<GenerateBatchTuitionChargesResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargesGroup.MapPost("/adhoc", async (
                CreateAdHocChargeRequest request,
                CreateAdHocChargeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/fin/charges/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Billing.ManageAdHoc)
            .AddEndpointFilter<ValidationFilter<CreateAdHocChargeRequest>>()
            .Produces<StudentChargeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargesGroup.MapPost("/{id:guid}/cancel", async (
                Guid id,
                CancelStudentChargeRequest request,
                CancelStudentChargeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Billing.Cancel)
            .AddEndpointFilter<ValidationFilter<CancelStudentChargeRequest>>()
            .Produces<StudentChargeDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargesGroup.MapGet("/", async (
                [AsParameters] GetStudentChargesQuery query,
                GetStudentChargesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Billing.Generate)
            .Produces<IReadOnlyList<StudentChargeDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
