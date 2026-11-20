using ElsasaSis.Contracts.Finance.Events;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Fin.Domain.Model;

public sealed class PaymentReceipt : AggregateRoot<Guid>
{
    private readonly List<PaymentAllocation> _allocations = new();

    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid AcademicYearId { get; private set; }
    public string AcademicYearName { get; private set; } = string.Empty;
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public Guid? StudyGroupId { get; private set; }
    public string? StudyGroupName { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? BankReference { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AllocatedAmount { get; private set; }
    public decimal UnallocatedAmount { get; private set; }
    public Guid ReceivedById { get; private set; }
    public string ReceivedByName { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    private PaymentReceipt()
    {
    }

    private PaymentReceipt(
        Guid id,
        string receiptNumber,
        Guid academicYearId,
        string academicYearName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? studyGroupId,
        string? studyGroupName,
        DateOnly paymentDate,
        string paymentMethod,
        string? bankReference,
        decimal totalAmount,
        decimal allocatedAmount,
        decimal unallocatedAmount,
        Guid receivedById,
        string receivedByName,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        ReceiptNumber = receiptNumber;
        AcademicYearId = academicYearId;
        AcademicYearName = academicYearName;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        StudyGroupId = studyGroupId;
        StudyGroupName = studyGroupName;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        BankReference = bankReference;
        TotalAmount = totalAmount;
        AllocatedAmount = allocatedAmount;
        UnallocatedAmount = unallocatedAmount;
        ReceivedById = receivedById;
        ReceivedByName = receivedByName;
        Notes = notes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Result<PaymentReceipt> Create(
        string receiptNumber,
        Guid academicYearId,
        string academicYearName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? studyGroupId,
        string? studyGroupName,
        DateOnly paymentDate,
        string paymentMethod,
        decimal totalAmount,
        Guid receivedById,
        string receivedByName,
        string? bankReference = null,
        string? notes = null,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            return Result<PaymentReceipt>.Failure(
                DomainError.Validation("Fin.PaymentReceipt.ReceiptNumberRequired", "Receipt number is required.", "receiptNumber"));
        }

        if (academicYearId == Guid.Empty || string.IsNullOrWhiteSpace(academicYearName))
        {
            return Result<PaymentReceipt>.Failure(
                DomainError.Validation("Fin.PaymentReceipt.AcademicYearRequired", "Academic year is required.", "academicYearId"));
        }

        if (studentId == Guid.Empty || string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(studentNis))
        {
            return Result<PaymentReceipt>.Failure(
                DomainError.Validation("Fin.PaymentReceipt.StudentRequired", "Student details are required.", "studentId"));
        }

        if (receivedById == Guid.Empty || string.IsNullOrWhiteSpace(receivedByName))
        {
            return Result<PaymentReceipt>.Failure(
                DomainError.Validation("Fin.PaymentReceipt.ReceivedByRequired", "Received-by cashier details are required.", "receivedById"));
        }

        var normalizedPaymentMethod = paymentMethod?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!PaymentMethods.IsValid(normalizedPaymentMethod))
        {
            return Result<PaymentReceipt>.Failure(FinErrors.InvalidPaymentMethod(paymentMethod ?? string.Empty));
        }

        if (totalAmount <= 0m)
        {
            return Result<PaymentReceipt>.Failure(FinErrors.InvalidPaymentAmount(totalAmount));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var receipt = new PaymentReceipt(
            UuidV7.NewGuid(),
            receiptNumber.Trim(),
            academicYearId,
            academicYearName.Trim(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            studyGroupId,
            string.IsNullOrWhiteSpace(studyGroupName) ? null : studyGroupName.Trim(),
            paymentDate,
            normalizedPaymentMethod,
            string.IsNullOrWhiteSpace(bankReference) ? null : bankReference.Trim(),
            totalAmount,
            0m,
            totalAmount,
            receivedById,
            receivedByName.Trim(),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            now,
            now);

        receipt.RaiseDomainEvent(new PaymentReceiptPostedDomainEvent(
            receipt.Id,
            receipt.ReceiptNumber,
            receipt.StudentId,
            receipt.TotalAmount,
            receipt.PaymentDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));

        return Result<PaymentReceipt>.Success(receipt);
    }

    public Result<PaymentAllocation> AllocateCharge(
        Guid chargeId,
        string chargeTypeName,
        string? billingMonth,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal amount,
        DateOnly allocationDate,
        DateTime? utcNow = null)
    {
        if (amount <= 0m)
        {
            return Result<PaymentAllocation>.Failure(FinErrors.InvalidPaymentAmount(amount));
        }

        if (AllocatedAmount + amount > TotalAmount)
        {
            return Result<PaymentAllocation>.Failure(
                FinErrors.ReceiptAllocationExceedsTotal(TotalAmount, AllocatedAmount + amount));
        }

        var now = utcNow ?? DateTime.UtcNow;
        var allocation = new PaymentAllocation(
            UuidV7.NewGuid(),
            Id,
            ReceiptNumber,
            chargeId,
            chargeTypeName.Trim(),
            string.IsNullOrWhiteSpace(billingMonth) ? null : billingMonth.Trim(),
            studentId,
            studentName.Trim(),
            studentNis.Trim(),
            amount,
            allocationDate,
            now,
            now);

        _allocations.Add(allocation);
        AllocatedAmount += amount;
        UnallocatedAmount = TotalAmount - AllocatedAmount;
        UpdatedAt = now;

        return Result<PaymentAllocation>.Success(allocation);
    }

    public static PaymentReceipt Rehydrate(
        Guid id,
        string receiptNumber,
        Guid academicYearId,
        string academicYearName,
        Guid studentId,
        string studentName,
        string studentNis,
        Guid? studyGroupId,
        string? studyGroupName,
        DateOnly paymentDate,
        string paymentMethod,
        string? bankReference,
        decimal totalAmount,
        decimal allocatedAmount,
        decimal unallocatedAmount,
        Guid receivedById,
        string receivedByName,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt,
        IEnumerable<PaymentAllocation>? allocations = null)
    {
        var receipt = new PaymentReceipt(
            id,
            receiptNumber,
            academicYearId,
            academicYearName,
            studentId,
            studentName,
            studentNis,
            studyGroupId,
            studyGroupName,
            paymentDate,
            paymentMethod,
            bankReference,
            totalAmount,
            allocatedAmount,
            unallocatedAmount,
            receivedById,
            receivedByName,
            notes,
            createdAt,
            updatedAt);

        if (allocations is not null)
        {
            receipt._allocations.AddRange(allocations);
        }

        return receipt;
    }
}
