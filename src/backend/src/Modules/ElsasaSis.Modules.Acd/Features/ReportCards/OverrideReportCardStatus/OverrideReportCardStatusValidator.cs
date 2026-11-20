using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.OverrideReportCardStatus;

public sealed class OverrideReportCardStatusValidator : AbstractValidator<OverrideReportCardStatusRequest>
{
    public OverrideReportCardStatusValidator()
    {
        RuleFor(x => x.OverrideReason)
            .NotEmpty()
            .WithMessage("Override reason is required when unlocking a finalized report card.")
            .MaximumLength(500)
            .WithMessage("Override reason cannot exceed 500 characters.");
    }
}
