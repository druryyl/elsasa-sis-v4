using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Fin.Domain;

public static class FinErrors
{
    public static DomainError StudentNotFound(Guid studentId) =>
        DomainError.NotFound(
            "Fin.StudentNotFound",
            $"Student with ID '{studentId}' was not found.");

    // Charge Types
    public static DomainError ChargeTypeNotFound(Guid id) =>
        DomainError.NotFound(
            "Fin.ChargeType.NotFound",
            $"Charge type with ID '{id}' was not found.");

    public static DomainError ChargeTypeAlreadyActive(Guid id) =>
        DomainError.Conflict(
            "Fin.ChargeType.AlreadyActive",
            $"Charge type with ID '{id}' is already active.");

    public static DomainError ChargeTypeCannotBeModified(Guid id) =>
        DomainError.Conflict(
            "Fin.ChargeType.CannotBeModified",
            $"Charge type with ID '{id}' is active and cannot be modified.");

    public static DomainError InvalidChargeTypeName() =>
        DomainError.Validation(
            "Fin.ChargeType.InvalidName",
            "Charge type name is required and cannot exceed 100 characters.",
            "name");

    public static DomainError InvalidChargeTypeAmount() =>
        DomainError.Validation(
            "Fin.ChargeType.InvalidAmount",
            "Charge type amount must be greater than or equal to zero.",
            "amount");

    public static DomainError InvalidChargeTypeFrequency(string frequency) =>
        DomainError.Validation(
            "Fin.ChargeType.InvalidFrequency",
            $"Frequency '{frequency}' is invalid. Allowed values: MONTHLY, SEMESTER, YEARLY, ONE_TIME.",
            "frequency");

    public static DomainError DuplicateChargeTypeName(string name, string yearName) =>
        DomainError.Conflict(
            "Fin.ChargeType.DuplicateName",
            $"Charge type with name '{name}' already exists for academic year '{yearName}'.");

    // Student Discounts
    public static DomainError StudentDiscountNotFound(Guid id) =>
        DomainError.NotFound(
            "Fin.StudentDiscount.NotFound",
            $"Student discount with ID '{id}' was not found.");

    public static DomainError StudentDiscountAlreadyExists(Guid studentId, Guid chargeTypeId, Guid academicYearId) =>
        DomainError.Conflict(
            "Fin.StudentDiscount.AlreadyExists",
            $"A discount for student '{studentId}', charge type '{chargeTypeId}', and academic year '{academicYearId}' already exists.");

    public static DomainError StudentDiscountAlreadyRevoked(Guid id) =>
        DomainError.Conflict(
            "Fin.StudentDiscount.AlreadyRevoked",
            $"Student discount with ID '{id}' is already revoked.");

    public static DomainError InvalidDiscountValue(string reason) =>
        DomainError.Validation(
            "Fin.StudentDiscount.InvalidValue",
            reason,
            "discountValue");

    public static DomainError InvalidDiscountType(string type) =>
        DomainError.Validation(
            "Fin.StudentDiscount.InvalidType",
            $"Discount type '{type}' is invalid. Allowed values: FIXED_AMOUNT, PERCENTAGE.",
            "discountType");

    // Student Charges
    public static DomainError StudentChargeNotFound(Guid id) =>
        DomainError.NotFound(
            "Fin.StudentCharge.NotFound",
            $"Student charge with ID '{id}' was not found.");

    public static DomainError StudentChargeAlreadyCancelled(Guid id) =>
        DomainError.Conflict(
            "Fin.StudentCharge.AlreadyCancelled",
            $"Student charge with ID '{id}' is already cancelled.");

    public static DomainError StudentChargeCannotBeCancelledWithPayments(Guid id, decimal paidAmount) =>
        DomainError.Conflict(
            "Fin.StudentCharge.CannotCancelPaidCharge",
            $"Student charge with ID '{id}' has paid amount of {paidAmount:N2} and cannot be cancelled.");

    public static DomainError MissingCancellationReason() =>
        DomainError.Validation(
            "Fin.StudentCharge.MissingCancellationReason",
            "Cancellation reason is required when cancelling a student charge.",
            "cancellationReason");

    public static DomainError PaymentAmountExceedsOutstanding(decimal paymentAmount, decimal outstandingAmount) =>
        DomainError.Validation(
            "Fin.StudentCharge.PaymentExceedsOutstanding",
            $"Payment amount {paymentAmount:N2} exceeds outstanding amount {outstandingAmount:N2}.",
            "amount");

    public static DomainError InvalidPaymentAmount(decimal amount) =>
        DomainError.Validation(
            "Fin.Payment.InvalidAmount",
            $"Payment amount must be greater than zero. Received: {amount:N2}.",
            "amount");

    // Payment Receipts
    public static DomainError PaymentReceiptNotFound(Guid id) =>
        DomainError.NotFound(
            "Fin.PaymentReceipt.NotFound",
            $"Payment receipt with ID '{id}' was not found.");

    public static DomainError ReceiptAllocationExceedsTotal(decimal totalAmount, decimal requestedAmount) =>
        DomainError.Validation(
            "Fin.PaymentReceipt.AllocationExceedsTotal",
            $"Allocations total {requestedAmount:N2} exceeds payment receipt total amount {totalAmount:N2}.",
            "allocatedAmount");

    public static DomainError ReceiptNumberAlreadyExists(string receiptNumber) =>
        DomainError.Conflict(
            "Fin.PaymentReceipt.DuplicateReceiptNumber",
            $"Receipt number '{receiptNumber}' already exists.");

    public static DomainError InvalidPaymentMethod(string method) =>
        DomainError.Validation(
            "Fin.PaymentReceipt.InvalidPaymentMethod",
            $"Payment method '{method}' is invalid. Allowed values: CASH, BANK_TRANSFER.",
            "paymentMethod");

    public static DomainError DepositBalanceInsufficient(decimal required, decimal available) =>
        DomainError.Conflict(
            "Fin.Deposit.InsufficientBalance",
            $"Available unallocated deposit balance of {available:N2} is insufficient for required allocation of {required:N2}.");

    public static DomainError Forbidden(string message = "You do not have permission to perform this action.") =>
        DomainError.Forbidden("Auth.Forbidden", message);
}
