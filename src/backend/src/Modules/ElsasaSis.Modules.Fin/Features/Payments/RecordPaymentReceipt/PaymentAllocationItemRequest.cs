namespace ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;

public sealed record PaymentAllocationItemRequest(
    Guid ChargeId,
    decimal Amount);
