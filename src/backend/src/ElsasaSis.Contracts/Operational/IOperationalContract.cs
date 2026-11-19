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

public interface IOperationalContract
{
    Task<ActiveAcademicYearDto?> GetActiveAcademicYearAsync(CancellationToken cancellationToken = default);
    Task<ActiveOperationalPeriodDto?> GetActiveOperationalPeriodAsync(CancellationToken cancellationToken = default);
}
