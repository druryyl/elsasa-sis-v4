using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Contracts.Finance.Events;

public sealed record PaymentReceiptPostedDomainEvent(
    Guid ReceiptId,
    string ReceiptNumber,
    Guid StudentId,
    decimal TotalAmount,
    DateTime PaymentDate) : DomainEventBase;
