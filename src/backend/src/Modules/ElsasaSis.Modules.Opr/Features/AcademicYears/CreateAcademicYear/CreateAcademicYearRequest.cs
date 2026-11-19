namespace ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;

public sealed record CreateAcademicYearRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<CreateOperationalPeriodRequest> Periods);

public sealed record CreateOperationalPeriodRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
