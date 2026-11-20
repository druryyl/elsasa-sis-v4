using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Features.Payments;

public sealed record PaymentAllocationDto(
    Guid Id,
    Guid PaymentReceiptId,
    string ReceiptNumber,
    Guid StudentChargeId,
    string ChargeTypeName,
    string? BillingMonth,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    decimal AllocatedAmount,
    DateOnly AllocationDate,
    DateTime CreatedAt)
{
    public static PaymentAllocationDto FromDomain(PaymentAllocation allocation) =>
        new(
            allocation.Id,
            allocation.PaymentReceiptId,
            allocation.ReceiptNumber,
            allocation.StudentChargeId,
            allocation.ChargeTypeName,
            allocation.BillingMonth,
            allocation.StudentId,
            allocation.StudentName,
            allocation.StudentNis,
            allocation.AllocatedAmount,
            allocation.AllocationDate,
            allocation.CreatedAt);
}
