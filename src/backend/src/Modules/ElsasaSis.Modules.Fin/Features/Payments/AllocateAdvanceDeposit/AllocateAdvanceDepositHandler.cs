using ElsasaSis.Modules.Fin.Domain;
using ElsasaSis.Modules.Fin.Domain.Model;
using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;

public sealed class AllocateAdvanceDepositHandler(
    IPaymentReceiptRepository paymentReceiptRepository,
    IStudentChargeRepository studentChargeRepository)
{
    public async Task<Result<PaymentReceiptDto>> HandleAsync(
        AllocateAdvanceDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceReceipt = await paymentReceiptRepository.GetByIdAsync(request.SourceReceiptId, cancellationToken);
        if (sourceReceipt is null)
        {
            return Result<PaymentReceiptDto>.Failure(
                FinErrors.PaymentReceiptNotFound(request.SourceReceiptId));
        }

        if (sourceReceipt.StudentId != request.StudentId)
        {
            return Result<PaymentReceiptDto>.Failure(
                DomainError.Conflict(
                    "Fin.Deposit.StudentMismatch",
                    $"Receipt '{sourceReceipt.ReceiptNumber}' belongs to student '{sourceReceipt.StudentId}', not '{request.StudentId}'."));
        }

        var totalRequestedAmount = request.Allocations.Sum(a => a.Amount);
        if (totalRequestedAmount > sourceReceipt.UnallocatedAmount)
        {
            return Result<PaymentReceiptDto>.Failure(
                FinErrors.DepositBalanceInsufficient(totalRequestedAmount, sourceReceipt.UnallocatedAmount));
        }

        var chargeIds = request.Allocations.Select(a => a.ChargeId).Distinct().ToList();
        var charges = await studentChargeRepository.GetByIdsAsync(chargeIds, cancellationToken);
        var chargesById = charges.ToDictionary(c => c.Id);

        var chargesToUpdate = new List<StudentCharge>();
        var allocationDate = request.AllocationDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var allocReq in request.Allocations)
        {
            if (!chargesById.TryGetValue(allocReq.ChargeId, out var charge))
            {
                return Result<PaymentReceiptDto>.Failure(
                    FinErrors.StudentChargeNotFound(allocReq.ChargeId));
            }

            if (charge.StudentId != request.StudentId)
            {
                return Result<PaymentReceiptDto>.Failure(
                    DomainError.Conflict(
                        "Fin.StudentCharge.StudentMismatch",
                        $"Charge '{charge.Id}' belongs to student '{charge.StudentId}', not '{request.StudentId}'."));
            }

            if (charge.Status == ChargeStatuses.Cancelled)
            {
                return Result<PaymentReceiptDto>.Failure(
                    FinErrors.StudentChargeAlreadyCancelled(charge.Id));
            }

            if (allocReq.Amount > charge.OutstandingAmount)
            {
                return Result<PaymentReceiptDto>.Failure(
                    FinErrors.PaymentAmountExceedsOutstanding(allocReq.Amount, charge.OutstandingAmount));
            }

            var applyResult = charge.ApplyPayment(allocReq.Amount);
            if (applyResult.IsFailure)
            {
                return Result<PaymentReceiptDto>.Failure(applyResult.Error);
            }

            var allocResult = sourceReceipt.AllocateCharge(
                charge.Id,
                charge.ChargeTypeName,
                charge.BillingMonth,
                charge.StudentId,
                charge.StudentName,
                charge.StudentNis,
                allocReq.Amount,
                allocationDate);

            if (allocResult.IsFailure)
            {
                return Result<PaymentReceiptDto>.Failure(allocResult.Error);
            }

            if (!chargesToUpdate.Contains(charge))
            {
                chargesToUpdate.Add(charge);
            }
        }

        await paymentReceiptRepository.SaveDepositAllocationTransactionAsync(sourceReceipt, chargesToUpdate, cancellationToken);

        return Result<PaymentReceiptDto>.Success(PaymentReceiptDto.FromDomain(sourceReceipt));
    }
}
