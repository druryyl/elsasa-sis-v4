using ElsasaSis.SharedKernel.Domain;

namespace ElsasaSis.Modules.Fin.Domain.Model;

public sealed class PaymentAllocation : Entity<Guid>
{
    public Guid PaymentReceiptId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public Guid StudentChargeId { get; private set; }
    public string ChargeTypeName { get; private set; } = string.Empty;
    public string? BillingMonth { get; private set; }
    public Guid StudentId { get; private set; }
    public string StudentName { get; private set; } = string.Empty;
    public string StudentNis { get; private set; } = string.Empty;
    public decimal AllocatedAmount { get; private set; }
    public DateOnly AllocationDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private PaymentAllocation()
    {
    }

    internal PaymentAllocation(
        Guid id,
        Guid paymentReceiptId,
        string receiptNumber,
        Guid studentChargeId,
        string chargeTypeName,
        string? billingMonth,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal allocatedAmount,
        DateOnly allocationDate,
        DateTime createdAt,
        DateTime updatedAt)
        : base(id)
    {
        PaymentReceiptId = paymentReceiptId;
        ReceiptNumber = receiptNumber;
        StudentChargeId = studentChargeId;
        ChargeTypeName = chargeTypeName;
        BillingMonth = billingMonth;
        StudentId = studentId;
        StudentName = studentName;
        StudentNis = studentNis;
        AllocatedAmount = allocatedAmount;
        AllocationDate = allocationDate;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static PaymentAllocation Rehydrate(
        Guid id,
        Guid paymentReceiptId,
        string receiptNumber,
        Guid studentChargeId,
        string chargeTypeName,
        string? billingMonth,
        Guid studentId,
        string studentName,
        string studentNis,
        decimal allocatedAmount,
        DateOnly allocationDate,
        DateTime createdAt,
        DateTime updatedAt)
    {
        return new PaymentAllocation(
            id,
            paymentReceiptId,
            receiptNumber,
            studentChargeId,
            chargeTypeName,
            billingMonth,
            studentId,
            studentName,
            studentNis,
            allocatedAmount,
            allocationDate,
            createdAt,
            updatedAt);
    }
}
