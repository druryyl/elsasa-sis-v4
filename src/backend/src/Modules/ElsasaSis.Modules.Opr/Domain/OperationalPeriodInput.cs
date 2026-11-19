namespace ElsasaSis.Modules.Opr.Domain;

public sealed record OperationalPeriodInput(
    Guid? Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate);
