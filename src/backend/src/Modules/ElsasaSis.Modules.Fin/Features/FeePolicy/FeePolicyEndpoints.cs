using ElsasaSis.Modules.Fin.Features.Discounts;
using ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;
using ElsasaSis.Modules.Fin.Features.Discounts.GetStudentDiscounts;
using ElsasaSis.Modules.Fin.Features.Discounts.RevokeStudentDiscount;
using ElsasaSis.Modules.Fin.Features.FeePolicy.ActivateChargeType;
using ElsasaSis.Modules.Fin.Features.FeePolicy.CreateChargeType;
using ElsasaSis.Modules.Fin.Features.FeePolicy.GetChargeTypes;
using ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy;

public sealed class FeePolicyEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Charge Types
        var chargeTypesGroup = app.MapGroup("/api/fin/charge-types")
            .WithTags("Finance - Fee Policies");

        chargeTypesGroup.MapPost("/", async (
                CreateChargeTypeRequest request,
                CreateChargeTypeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/fin/charge-types/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.FeePolicy.Manage)
            .AddEndpointFilter<ValidationFilter<CreateChargeTypeRequest>>()
            .Produces<ChargeTypeDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargeTypesGroup.MapGet("/", async (
                [AsParameters] GetChargeTypesQuery query,
                GetChargeTypesHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.FeePolicy.Manage)
            .Produces<IReadOnlyList<ChargeTypeDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        chargeTypesGroup.MapPut("/{id:guid}", async (
                Guid id,
                UpdateChargeTypeRequest request,
                UpdateChargeTypeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.FeePolicy.Manage)
            .AddEndpointFilter<ValidationFilter<UpdateChargeTypeRequest>>()
            .Produces<ChargeTypeDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        chargeTypesGroup.MapPost("/{id:guid}/activate", async (
                Guid id,
                ActivateChargeTypeHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.FeePolicy.Manage)
            .Produces<ChargeTypeDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Student Discounts
        var discountsGroup = app.MapGroup("/api/fin/discounts")
            .WithTags("Finance - Discounts");

        discountsGroup.MapPost("/", async (
                CreateStudentDiscountRequest request,
                CreateStudentDiscountHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/fin/discounts/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Discounts.Manage)
            .AddEndpointFilter<ValidationFilter<CreateStudentDiscountRequest>>()
            .Produces<StudentDiscountDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        discountsGroup.MapGet("/", async (
                [AsParameters] GetStudentDiscountsQuery query,
                GetStudentDiscountsHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Discounts.Manage)
            .Produces<IReadOnlyList<StudentDiscountDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        discountsGroup.MapPost("/{id:guid}/revoke", async (
                Guid id,
                RevokeStudentDiscountHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(id, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Discounts.Manage)
            .Produces<StudentDiscountDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
