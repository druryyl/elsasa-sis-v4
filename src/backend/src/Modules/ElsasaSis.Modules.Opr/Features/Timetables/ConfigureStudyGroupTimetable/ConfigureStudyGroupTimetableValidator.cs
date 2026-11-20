using ElsasaSis.Modules.Opr.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ConfigureStudyGroupTimetable;

public sealed class ConfigureStudyGroupTimetableValidator : AbstractValidator<ConfigureStudyGroupTimetableRequest>
{
    public ConfigureStudyGroupTimetableValidator()
    {
        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty()
            .WithMessage("Operational period ID is required.");

        RuleFor(x => x.ScheduleType)
            .NotEmpty()
            .WithMessage("Schedule type is required.")
            .Must(type => ScheduleTypeExtensions.Parse(type).IsSuccess)
            .WithMessage("Schedule type must be REGULAR, GENERAL, or VOCATIONAL.");

        RuleFor(x => x.DayOfWeek)
            .InclusiveBetween(1, 7)
            .WithMessage("Day of week must be between 1 and 7.");

        RuleFor(x => x.PeriodSlot)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Period slot must be greater than or equal to 1.");

        RuleFor(x => x.SubjectId)
            .NotEmpty()
            .WithMessage("Subject ID is required.");

        RuleFor(x => x.TeacherId)
            .NotEmpty()
            .WithMessage("Teacher ID is required.");

        RuleFor(x => x.EndTime)
            .Must((req, endTime) => endTime > req.StartTime)
            .WithMessage("End time must be strictly after start time.");
    }
}
