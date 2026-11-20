using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Acd.Domain.Model;

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Excused = 3,
    Sick = 4,
    Late = 5
}

public static class AttendanceStatusExtensions
{
    public static string ToCode(this AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "PRESENT",
        AttendanceStatus.Absent => "ABSENT",
        AttendanceStatus.Excused => "EXCUSED",
        AttendanceStatus.Sick => "SICK",
        AttendanceStatus.Late => "LATE",
        _ => status.ToString().ToUpperInvariant()
    };

    public static Result<AttendanceStatus> ParseAttendanceStatus(string? raw, string field = "status")
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result<AttendanceStatus>.Failure(AttendanceErrors.InvalidStatus(raw, field));
        }

        var normalized = raw.Trim().ToUpperInvariant();
        return normalized switch
        {
            "PRESENT" => Result<AttendanceStatus>.Success(AttendanceStatus.Present),
            "ABSENT" => Result<AttendanceStatus>.Success(AttendanceStatus.Absent),
            "EXCUSED" => Result<AttendanceStatus>.Success(AttendanceStatus.Excused),
            "SICK" => Result<AttendanceStatus>.Success(AttendanceStatus.Sick),
            "LATE" => Result<AttendanceStatus>.Success(AttendanceStatus.Late),
            _ => Result<AttendanceStatus>.Failure(AttendanceErrors.InvalidStatus(raw, field))
        };
    }
}
