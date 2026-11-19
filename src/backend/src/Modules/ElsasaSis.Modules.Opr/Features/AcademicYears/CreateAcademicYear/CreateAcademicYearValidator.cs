using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.AcademicYears.CreateAcademicYear;

public sealed class CreateAcademicYearValidator : AbstractValidator<CreateAcademicYearRequest>
{
    public CreateAcademicYearValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.StartDate)
            .NotEmpty();

        RuleFor(x => x.EndDate)
            .NotEmpty();

        RuleFor(x => x.Periods)
            .NotNull()
            .NotEmpty();

        RuleForEach(x => x.Periods).ChildRules(period =>
        {
            period.RuleFor(p => p.Name)
                .NotEmpty()
                .MaximumLength(50);

            period.RuleFor(p => p.StartDate)
                .NotEmpty();

            period.RuleFor(p => p.EndDate)
                .NotEmpty();
        });
    }
}
