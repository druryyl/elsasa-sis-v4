using ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;
using ElsasaSis.Modules.Fin.Features.Payments.GetPaymentReceiptById;
using ElsasaSis.Modules.Fin.Features.Payments.GetStudentStatement;
using ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ElsasaSis.Modules.Fin.Features.Payments;

public sealed class PaymentsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var paymentsGroup = app.MapGroup("/api/fin")
            .WithTags("Finance - Payments & Cashier");

        paymentsGroup.MapPost("/payments", async (
                RecordPaymentReceiptRequest request,
                RecordPaymentReceiptHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/fin/payments/{result.Value!.Id}", result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Payments.Record)
            .AddEndpointFilter<ValidationFilter<RecordPaymentReceiptRequest>>()
            .Produces<PaymentReceiptDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        paymentsGroup.MapPost("/deposits/allocate", async (
                AllocateAdvanceDepositRequest request,
                AllocateAdvanceDepositHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(request, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Deposits.Allocate)
            .AddEndpointFilter<ValidationFilter<AllocateAdvanceDepositRequest>>()
            .Produces<PaymentReceiptDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        paymentsGroup.MapGet("/students/{studentId:guid}/statement", async (
                Guid studentId,
                GetStudentStatementHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new GetStudentStatementQuery(studentId), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Statements.View)
            .Produces<StudentStatementDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        paymentsGroup.MapGet("/payments/{receiptId:guid}", async (
                Guid receiptId,
                GetPaymentReceiptByIdHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(new GetPaymentReceiptByIdQuery(receiptId), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Error.ToProblemDetails();
            })
            .RequireAuthorization(Permissions.Finance.Statements.View)
            .Produces<PaymentReceiptDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
