namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed record CreateSmkBlockRequest(
    Guid OperationalPeriodId,
    string ScheduleType,
    DateOnly StartDate,
    DateOnly EndDate);
