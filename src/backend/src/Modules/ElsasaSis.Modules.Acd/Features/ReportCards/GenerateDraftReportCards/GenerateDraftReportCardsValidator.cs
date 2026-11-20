using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.GenerateDraftReportCards;

public sealed class GenerateDraftReportCardsValidator : AbstractValidator<GenerateDraftReportCardsRequest>
{
    public GenerateDraftReportCardsValidator()
    {
        RuleFor(x => x.StudyGroupId)
            .NotEmpty()
            .WithMessage("Study group identifier is required.");

        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty()
            .WithMessage("Operational period identifier is required.");
    }
}
