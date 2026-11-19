using ElsasaSis.SharedKernel.Common;

namespace ElsasaSis.Modules.Opr.Domain;

public static class OprErrors
{
    public static DomainError NotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.AcademicYear.NotFound",
            $"Academic year '{id}' was not found.");

    public static DomainError PeriodNotFound(Guid periodId) =>
        DomainError.NotFound(
            "Opr.OperationalPeriod.NotFound",
            $"Operational period '{periodId}' was not found.");

    public static DomainError DuplicateYearName(string name) =>
        DomainError.Conflict(
            "Opr.AcademicYear.DuplicateName",
            $"An academic year named '{name}' already exists.");

    public static DomainError DuplicatePeriodName(string name) =>
        DomainError.Validation(
            "Opr.OperationalPeriod.DuplicateName",
            $"Operational period name '{name}' is duplicated within the academic year.",
            "periods");

    public static DomainError InvalidDateRange(string field = "endDate") =>
        DomainError.Validation(
            "Opr.DateRange.Invalid",
            "End date must be strictly after start date.",
            field);

    public static DomainError PeriodOutOfBounds(string periodName) =>
        DomainError.Validation(
            "Opr.OperationalPeriod.OutOfBounds",
            $"Operational period '{periodName}' must fall within the academic year date range.",
            "periods");

    public static DomainError OverlappingPeriods(string firstPeriodName, string secondPeriodName) =>
        DomainError.Validation(
            "Opr.OperationalPeriod.Overlapping",
            $"Operational periods '{firstPeriodName}' and '{secondPeriodName}' overlap.",
            "periods");

    public static DomainError AtLeastOnePeriodRequired() =>
        DomainError.Validation(
            "Opr.AcademicYear.AtLeastOnePeriodRequired",
            "An academic year must contain at least one operational period.",
            "periods");

    public static DomainError CannotRemoveActivePeriod(Guid periodId) =>
        DomainError.Conflict(
            "Opr.OperationalPeriod.CannotRemoveActive",
            $"Cannot remove the currently active operational period '{periodId}'.");

    public static DomainError YearNotActiveForPeriodActivation(Guid academicYearId) =>
        DomainError.Conflict(
            "Opr.AcademicYear.NotActiveForPeriodActivation",
            $"Academic year '{academicYearId}' must be active before activating an operational period.");

    public static DomainError YearNameRequired() =>
        DomainError.Validation(
            "Opr.AcademicYear.NameRequired",
            "Academic year name is required.",
            "name");

    public static DomainError PeriodNameRequired() =>
        DomainError.Validation(
            "Opr.OperationalPeriod.NameRequired",
            "Operational period name is required.",
            "periods");

    public static DomainError NoActiveAcademicYear() =>
        DomainError.Conflict(
            "Opr.AcademicYear.NoneActive",
            "No active academic year is currently configured.");

    public static DomainError CalendarEntryNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.CalendarEntry.NotFound",
            $"Calendar entry '{id}' was not found.");

    public static DomainError CalendarEntryAcademicYearRequired() =>
        DomainError.Validation(
            "Opr.CalendarEntry.AcademicYearRequired",
            "Academic year ID is required.",
            "academicYearId");

    public static DomainError DuplicateCalendarEntryDate(Guid academicYearId, DateOnly entryDate) =>
        DomainError.Conflict(
            "Opr.CalendarEntry.DuplicateDate",
            $"A calendar entry already exists for date '{entryDate:yyyy-MM-dd}' in academic year '{academicYearId}'.");

    public static DomainError CalendarEntryDateOutOfBounds(DateOnly entryDate, DateOnly startDate, DateOnly endDate) =>
        DomainError.Validation(
            "Opr.CalendarEntry.DateOutOfBounds",
            $"Calendar entry date '{entryDate:yyyy-MM-dd}' must fall within the academic year range '{startDate:yyyy-MM-dd}' to '{endDate:yyyy-MM-dd}'.",
            "entryDate");

    public static DomainError InvalidCalendarDayType(string? dayType) =>
        DomainError.Validation(
            "Opr.CalendarEntry.InvalidDayType",
            $"'{dayType}' is not a valid calendar day type. Allowed values: SCHOOL_DAY, HOLIDAY, CLOSURE.",
            "dayType");

    public static DomainError RoomNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Room.NotFound",
            $"Room '{id}' was not found.");

    public static DomainError RoomNameRequired() =>
        DomainError.Validation(
            "Opr.Room.NameRequired",
            "Room name is required.",
            "name");

    public static DomainError DuplicateRoomName(string name) =>
        DomainError.Conflict(
            "Opr.Room.DuplicateName",
            $"A room named '{name}' already exists.");

    public static DomainError InvalidRoomType(string? roomType) =>
        DomainError.Validation(
            "Opr.Room.InvalidRoomType",
            $"'{roomType}' is not a valid room type. Allowed values: CLASSROOM, LABORATORY, HALL, OTHER.",
            "roomType");

    public static DomainError InvalidRoomCapacity() =>
        DomainError.Validation(
            "Opr.Room.InvalidCapacity",
            "Room capacity must be greater than zero.",
            "capacity");

    public static DomainError AnnouncementNotFound(Guid id) =>
        DomainError.NotFound(
            "Opr.Announcement.NotFound",
            $"Announcement '{id}' was not found.");

    public static DomainError AnnouncementAcademicYearRequired() =>
        DomainError.Validation(
            "Opr.Announcement.AcademicYearRequired",
            "Academic year ID is required.",
            "academicYearId");

    public static DomainError AnnouncementAuthorRequired() =>
        DomainError.Validation(
            "Opr.Announcement.AuthorRequired",
            "Announcement author is required.",
            "authorId");

    public static DomainError AnnouncementTitleRequired() =>
        DomainError.Validation(
            "Opr.Announcement.TitleRequired",
            "Announcement title is required.",
            "title");

    public static DomainError AnnouncementContentRequired() =>
        DomainError.Validation(
            "Opr.Announcement.ContentRequired",
            "Announcement content is required.",
            "content");

    public static DomainError InvalidAnnouncementAudience(string? targetAudience) =>
        DomainError.Validation(
            "Opr.Announcement.InvalidAudience",
            $"'{targetAudience}' is not a valid target audience. Allowed values: ALL, TEACHERS, STUDENTS, PARENTS.",
            "targetAudience");

    public static DomainError InvalidAnnouncementStatus(string? status) =>
        DomainError.Validation(
            "Opr.Announcement.InvalidStatus",
            $"'{status}' is not a valid announcement status. Allowed values: DRAFT, PUBLISHED.",
            "status");

    public static DomainError AnnouncementAlreadyPublished(Guid id) =>
        DomainError.Conflict(
            "Opr.Announcement.AlreadyPublished",
            $"Announcement '{id}' has already been published.");

    public static DomainError AnnouncementForbidden(Guid id) =>
        DomainError.Forbidden(
            "Opr.Announcement.Forbidden",
            $"You do not have permission to access announcement '{id}'.");
}
