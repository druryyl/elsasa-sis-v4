namespace ElsasaSis.Contracts.Operational;

public sealed record ActiveAcademicYearDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record ActiveOperationalPeriodDto(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);

public sealed record AcademicYearSummaryDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

public sealed record OperationalPeriodSummaryDto(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);

public interface IOperationalContract
{
    Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default);
    Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default);
    Task<AcademicYearSummaryDto?> GetAcademicYearByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OperationalPeriodSummaryDto?> GetOperationalPeriodByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
