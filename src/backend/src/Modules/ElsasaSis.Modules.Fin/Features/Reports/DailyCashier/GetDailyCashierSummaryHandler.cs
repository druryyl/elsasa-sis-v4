using Dapper;
using ElsasaSis.SharedKernel.Common;
using ElsasaSis.SharedKernel.Persistence;

namespace ElsasaSis.Modules.Fin.Features.Reports.DailyCashier;

public sealed class GetDailyCashierSummaryHandler(ISqlConnectionFactory connectionFactory)
{
    private const string SelectDailyReceiptsSql = """
        SELECT
            id AS Id,
            receipt_number AS ReceiptNumber,
            academic_year_id AS AcademicYearId,
            academic_year_name AS AcademicYearName,
            student_id AS StudentId,
            student_name AS StudentName,
            student_nis AS StudentNis,
            study_group_id AS StudyGroupId,
            study_group_name AS StudyGroupName,
            payment_date AS PaymentDate,
            payment_method AS PaymentMethod,
            bank_reference AS BankReference,
            total_amount AS TotalAmount,
            allocated_amount AS AllocatedAmount,
            unallocated_amount AS UnallocatedAmount,
            received_by_id AS ReceivedById,
            received_by_name AS ReceivedByName,
            notes AS Notes,
            created_at AS CreatedAt
        FROM fin.payment_receipts
        WHERE payment_date = @Date
        ORDER BY created_at DESC;
        """;

    public async Task<Result<DailyCashierSummaryDto>> HandleAsync(
        GetDailyCashierSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var targetDate = query.Date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var targetDateTime = targetDate.ToDateTime(TimeOnly.MinValue);

        using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<ReceiptDbRow>(new CommandDefinition(
            SelectDailyReceiptsSql,
            new { Date = targetDateTime },
            cancellationToken: cancellationToken))).ToList();

        var receipts = rows.Select(r => new DailyCashierReceiptItemDto(
            r.Id,
            r.ReceiptNumber,
            r.AcademicYearId,
            r.AcademicYearName,
            r.StudentId,
            r.StudentName,
            r.StudentNis,
            r.StudyGroupId,
            r.StudyGroupName,
            DateOnly.FromDateTime(r.PaymentDate),
            r.PaymentMethod,
            r.BankReference,
            r.TotalAmount,
            r.AllocatedAmount,
            r.UnallocatedAmount,
            r.ReceivedById,
            r.ReceivedByName,
            r.Notes,
            r.CreatedAt)).ToList();

        var totalCash = receipts
            .Where(r => string.Equals(r.PaymentMethod, "CASH", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.TotalAmount);

        var totalBank = receipts
            .Where(r => string.Equals(r.PaymentMethod, "BANK_TRANSFER", StringComparison.OrdinalIgnoreCase))
            .Sum(r => r.TotalAmount);

        var totalCollections = receipts.Sum(r => r.TotalAmount);
        var totalDepositsHeld = receipts.Sum(r => r.UnallocatedAmount);
        var totalAllocated = receipts.Sum(r => r.AllocatedAmount);

        var methodSummaries = receipts
            .GroupBy(r => r.PaymentMethod, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PaymentMethodSummaryDto(
                g.Key.ToUpperInvariant(),
                g.Sum(x => x.TotalAmount),
                g.Count()))
            .OrderBy(x => x.PaymentMethod)
            .ToList();

        var dto = new DailyCashierSummaryDto(
            targetDate,
            totalCash,
            totalBank,
            totalCollections,
            totalDepositsHeld,
            totalAllocated,
            receipts.Count,
            methodSummaries,
            receipts);

        return Result<DailyCashierSummaryDto>.Success(dto);
    }

    private sealed record ReceiptDbRow(
        Guid Id,
        string ReceiptNumber,
        Guid AcademicYearId,
        string AcademicYearName,
        Guid StudentId,
        string StudentName,
        string StudentNis,
        Guid? StudyGroupId,
        string? StudyGroupName,
        DateTime PaymentDate,
        string PaymentMethod,
        string? BankReference,
        decimal TotalAmount,
        decimal AllocatedAmount,
        decimal UnallocatedAmount,
        Guid ReceivedById,
        string ReceivedByName,
        string? Notes,
        DateTime CreatedAt);
}
