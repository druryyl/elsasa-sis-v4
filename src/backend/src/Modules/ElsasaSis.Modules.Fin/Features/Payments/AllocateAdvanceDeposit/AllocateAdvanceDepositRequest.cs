using ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;

namespace ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;

public sealed record AllocateAdvanceDepositRequest(
    Guid StudentId,
    Guid SourceReceiptId,
    IReadOnlyList<PaymentAllocationItemRequest> Allocations,
    DateOnly? AllocationDate = null);
