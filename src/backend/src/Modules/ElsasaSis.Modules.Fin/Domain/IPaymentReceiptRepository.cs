using ElsasaSis.Modules.Fin.Domain.Model;

namespace ElsasaSis.Modules.Fin.Domain;

public interface IPaymentReceiptRepository
{
    Task<PaymentReceipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentReceipt?> GetByReceiptNumberAsync(string receiptNumber, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentReceipt>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentReceipt>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task InsertAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentReceipt receipt, CancellationToken cancellationToken = default);
    Task SavePaymentReceiptTransactionAsync(
        PaymentReceipt receipt,
        IReadOnlyList<StudentCharge> updatedCharges,
        CancellationToken cancellationToken = default);
    Task SaveDepositAllocationTransactionAsync(
        PaymentReceipt sourceReceipt,
        IReadOnlyList<StudentCharge> updatedCharges,
        CancellationToken cancellationToken = default);
}
