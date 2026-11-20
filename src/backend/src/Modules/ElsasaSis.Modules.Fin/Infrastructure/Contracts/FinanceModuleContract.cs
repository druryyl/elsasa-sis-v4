using ElsasaSis.Contracts.Finance;

namespace ElsasaSis.Modules.Fin.Infrastructure.Contracts;

public sealed class FinanceModuleContract : IFinanceModule
{
    public Task<StudentFinancialSummary?> GetStudentFinancialSummaryAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<StudentFinancialSummary?>(null);
    }
}
