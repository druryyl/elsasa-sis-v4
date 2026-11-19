using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.CreateCalendarEntry;

public sealed class CreateCalendarEntryValidator : AbstractValidator<CreateCalendarEntryRequest>
{
    public CreateCalendarEntryValidator()
    {
        RuleFor(x => x.AcademicYearId)
            .NotEmpty();

        RuleFor(x => x.EntryDate)
            .NotEmpty();

        RuleFor(x => x.DayType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}
