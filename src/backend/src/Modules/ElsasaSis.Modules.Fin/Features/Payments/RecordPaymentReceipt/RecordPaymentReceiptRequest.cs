namespace ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;

public sealed record RecordPaymentReceiptRequest(
    Guid StudentId,
    Guid AcademicYearId,
    DateOnly PaymentDate,
    string PaymentMethod,
    decimal TotalAmount,
    IReadOnlyList<PaymentAllocationItemRequest>? Allocations = null,
    string? BankReference = null,
    string? Notes = null,
    string? StudentName = null,
    string? StudentNis = null,
    string? AcademicYearName = null,
    Guid? StudyGroupId = null,
    string? StudyGroupName = null,
    Guid? ReceivedById = null,
    string? ReceivedByName = null);
