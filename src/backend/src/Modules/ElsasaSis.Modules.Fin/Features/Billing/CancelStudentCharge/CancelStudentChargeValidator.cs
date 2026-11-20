using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Billing.CancelStudentCharge;

public sealed class CancelStudentChargeValidator : AbstractValidator<CancelStudentChargeRequest>
{
    public CancelStudentChargeValidator()
    {
        RuleFor(x => x.CancellationReason)
            .NotEmpty()
            .WithMessage("Cancellation reason is required.")
            .MaximumLength(500)
            .WithMessage("Cancellation reason cannot exceed 500 characters.");
    }
}
