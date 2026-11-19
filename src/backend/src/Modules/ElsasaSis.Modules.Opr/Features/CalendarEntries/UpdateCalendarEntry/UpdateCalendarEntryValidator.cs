using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.CalendarEntries.UpdateCalendarEntry;

public sealed class UpdateCalendarEntryValidator : AbstractValidator<UpdateCalendarEntryRequest>
{
    public UpdateCalendarEntryValidator()
    {
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
