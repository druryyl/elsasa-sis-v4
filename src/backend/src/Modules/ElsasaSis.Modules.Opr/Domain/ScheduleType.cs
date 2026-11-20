using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public enum ScheduleType
{
    Regular = 0,
    General = 1,
    Vocational = 2
}

public static class ScheduleTypeExtensions
{
    public const string RegularCode = "REGULAR";
    public const string GeneralCode = "GENERAL";
    public const string VocationalCode = "VOCATIONAL";

    public static string ToCode(this ScheduleType scheduleType) => scheduleType switch
    {
        ScheduleType.Regular => RegularCode,
        ScheduleType.General => GeneralCode,
        ScheduleType.Vocational => VocationalCode,
        _ => throw new ArgumentOutOfRangeException(nameof(scheduleType), scheduleType, "Unsupported schedule type.")
    };

    public static Result<ScheduleType> Parse(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            RegularCode => Result<ScheduleType>.Success(ScheduleType.Regular),
            GeneralCode => Result<ScheduleType>.Success(ScheduleType.General),
            VocationalCode => Result<ScheduleType>.Success(ScheduleType.Vocational),
            _ => Result<ScheduleType>.Failure(TimetableErrors.InvalidScheduleType(raw))
        };
    }

    public static Result<ScheduleType> ParseBlockScheduleType(string? raw)
    {
        return raw?.Trim().ToUpperInvariant() switch
        {
            GeneralCode => Result<ScheduleType>.Success(ScheduleType.General),
            VocationalCode => Result<ScheduleType>.Success(ScheduleType.Vocational),
            _ => Result<ScheduleType>.Failure(TimetableErrors.InvalidBlockScheduleType(raw))
        };
    }
}
