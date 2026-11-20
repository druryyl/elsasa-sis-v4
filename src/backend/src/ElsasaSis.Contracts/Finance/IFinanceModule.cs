namespace ElsasaSis.Contracts.Finance;

public sealed record StudentFinancialSummary(
    Guid StudentId,
    decimal TotalCharged,
    decimal TotalPaid,
    decimal TotalOutstanding,
    decimal DepositBalance);

public interface IFinanceModule
{
    Task<StudentFinancialSummary?> GetStudentFinancialSummaryAsync(Guid studentId, CancellationToken cancellationToken = default);
}
