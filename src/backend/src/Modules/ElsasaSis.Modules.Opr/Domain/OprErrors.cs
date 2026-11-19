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
}
