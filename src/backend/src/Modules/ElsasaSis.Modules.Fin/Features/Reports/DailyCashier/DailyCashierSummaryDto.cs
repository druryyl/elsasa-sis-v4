namespace ElsasaSis.Modules.Fin.Features.Reports.DailyCashier;

public sealed record DailyCashierSummaryDto(
    DateOnly Date,
    decimal TotalCashCollections,
    decimal TotalBankTransferCollections,
    decimal TotalCollections,
    decimal TotalDepositsHeld,
    decimal TotalAllocated,
    int ReceiptCount,
    IReadOnlyList<PaymentMethodSummaryDto> PaymentMethodSummaries,
    IReadOnlyList<DailyCashierReceiptItemDto> Receipts);

public sealed record PaymentMethodSummaryDto(
    string PaymentMethod,
    decimal TotalAmount,
    int ReceiptCount);

public sealed record DailyCashierReceiptItemDto(
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
    DateTime CreatedAt);
