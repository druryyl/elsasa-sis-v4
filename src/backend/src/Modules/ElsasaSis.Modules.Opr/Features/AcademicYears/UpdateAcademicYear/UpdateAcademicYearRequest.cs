namespace ElsasaSis.Modules.Opr.Features.AcademicYears.UpdateAcademicYear;

public sealed record UpdateAcademicYearRequest(
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<UpdateOperationalPeriodRequest> Periods);

public sealed record UpdateOperationalPeriodRequest(
    Guid? Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
