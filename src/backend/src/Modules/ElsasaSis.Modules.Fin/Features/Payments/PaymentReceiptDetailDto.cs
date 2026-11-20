namespace ElsasaSis.Modules.Fin.Features.Payments;

public sealed record PaymentReceiptDetailDto(
    Guid Id,
    string ReceiptNumber,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    DateOnly PaymentDate,
    string PaymentMethod,
    string? BankReference,
    decimal TotalAmount,
    decimal AllocatedAmount,
    decimal UnallocatedAmount,
    Guid ReceivedById,
    string ReceivedByName,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<PaymentReceiptAllocationDetailDto> Allocations);

public sealed record PaymentReceiptAllocationDetailDto(
    Guid Id,
    Guid StudentChargeId,
    string ChargeTypeName,
    string? BillingMonth,
    decimal AllocatedAmount,
    DateOnly AllocationDate,
    DateTime CreatedAt);
