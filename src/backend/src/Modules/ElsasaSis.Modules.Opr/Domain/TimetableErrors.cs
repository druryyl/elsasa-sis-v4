using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public static class TimetableErrors
{
    public static DomainError ScheduleNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.ScheduleNotFound",
            $"Study group schedule slot '{id}' was not found.");

    public static DomainError BlockNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.BlockNotFound",
            $"Study group timetable block '{id}' was not found.");

    public static DomainError StudyGroupNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.StudyGroupNotFound",
            $"Study group '{id}' was not found.");

    public static DomainError OperationalPeriodNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.OperationalPeriodNotFound",
            $"Operational period '{id}' was not found.");

    public static DomainError OperationalPeriodNotActive(Guid id) =>
        DomainError.Conflict(
            "Opr.Timetable.OperationalPeriodNotActive",
            $"Operational period '{id}' is not currently active.");

    public static DomainError SubjectNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.SubjectNotFound",
            $"Subject '{id}' was not found.");

    public static DomainError TeacherNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.TeacherNotFound",
            $"Teacher '{id}' was not found.");

    public static DomainError RoomNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Timetable.RoomNotFound",
            $"Room '{id}' was not found.");

    public static DomainError InvalidTimeRange(string field = "endTime") =>
        DomainError.Validation(
            "Opr.Timetable.InvalidTimeRange",
            "End time must be strictly after start time.",
            field);

    public static DomainError InvalidDateRange(string field = "endDate") =>
        DomainError.Validation(
            "Opr.Timetable.InvalidDateRange",
            "End date must be on or after start date.",
            field);

    public static DomainError InvalidDayOfWeek(int dayOfWeek) =>
        DomainError.Validation(
            "Opr.Timetable.InvalidDayOfWeek",
            $"Day of week '{dayOfWeek}' is invalid. Must be between 1 and 7.",
            "dayOfWeek");

    public static DomainError InvalidPeriodSlot(int slot) =>
        DomainError.Validation(
            "Opr.Timetable.InvalidPeriodSlot",
            $"Period slot '{slot}' must be greater than or equal to 1.",
            "periodSlot");

    public static DomainError InvalidScheduleType(string? raw) =>
        DomainError.Validation(
            "Opr.Timetable.InvalidScheduleType",
            $"Schedule type '{raw}' is invalid. Allowed values: REGULAR, GENERAL, VOCATIONAL.",
            "scheduleType");

    public static DomainError InvalidBlockScheduleType(string? raw) =>
        DomainError.Validation(
            "Opr.Timetable.InvalidBlockScheduleType",
            $"SMK block rotation schedule type '{raw}' is invalid. Allowed values: GENERAL, VOCATIONAL.",
            "scheduleType");

    public static DomainError TeacherNotAssignedToUser() =>
        DomainError.Forbidden(
            "Opr.Timetable.TeacherNotAssignedToUser",
            "The authenticated user is not associated with an active teacher profile.");

    public static DomainError FieldRequired(string fieldName) =>
        DomainError.Validation(
            "Opr.Timetable.FieldRequired",
            $"Field '{fieldName}' is required.",
            fieldName);
}
