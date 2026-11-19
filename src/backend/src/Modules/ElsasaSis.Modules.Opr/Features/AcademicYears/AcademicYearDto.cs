using ElsasaSis.Modules.Opr.Domain;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears;

public sealed record AcademicYearDto(
    Guid Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<OperationalPeriodDto> Periods);

public sealed record OperationalPeriodDto(
    Guid Id,
    Guid AcademicYearId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

internal static class AcademicYearMappings
{
    public static AcademicYearDto ToDto(this AcademicYear academicYear) =>
        new(
            academicYear.Id,
            academicYear.Name,
            academicYear.StartDate,
            academicYear.EndDate,
            academicYear.Status.ToString().ToUpperInvariant(),
            academicYear.CreatedAt,
            academicYear.UpdatedAt,
            academicYear.Periods
                .OrderBy(p => p.StartDate)
                .ThenBy(p => p.EndDate)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(p => p.ToDto())
                .ToArray());

    public static OperationalPeriodDto ToDto(this OperationalPeriod period) =>
        new(
            period.Id,
            period.AcademicYearId,
            period.Name,
            period.StartDate,
            period.EndDate,
            period.Status.ToString().ToUpperInvariant(),
            period.CreatedAt,
            period.UpdatedAt);
}
