namespace ElsasaSis.Modules.Fin.Features.Payments;

public sealed record StudentStatementDto(
    Guid StudentId,
    string StudentName,
    string StudentNis,
    Guid? StudyGroupId,
    string? StudyGroupName,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstanding,
    decimal TotalDepositBalance,
    IReadOnlyList<StudentStatementChargeDto> Charges,
    IReadOnlyList<StudentStatementReceiptDto> Receipts);

public sealed record StudentStatementChargeDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid? OperationalPeriodId,
    string? OperationalPeriodName,
    string? BillingMonth,
    Guid ChargeTypeId,
    string ChargeTypeName,
    string ChargeFrequency,
    string? Description,
    decimal UnitAmount,
    int Quantity,
    decimal GrossAmount,
    decimal DiscountAmount,
    string? DiscountReason,
    decimal NetAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    string Status,
    DateOnly? DueDate,
    DateTime CreatedAt);

public sealed record StudentStatementReceiptDto(
    Guid Id,
    string ReceiptNumber,
    Guid AcademicYearId,
    string AcademicYearName,
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
    IReadOnlyList<StudentStatementAllocationDto> Allocations);

public sealed record StudentStatementAllocationDto(
    Guid Id,
    Guid StudentChargeId,
    string ChargeTypeName,
    string? BillingMonth,
    decimal AllocatedAmount,
    DateOnly AllocationDate,
    DateTime CreatedAt);
