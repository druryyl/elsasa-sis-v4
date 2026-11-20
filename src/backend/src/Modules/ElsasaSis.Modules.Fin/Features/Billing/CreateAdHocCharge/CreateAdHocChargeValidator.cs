using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Billing.CreateAdHocCharge;

public sealed class CreateAdHocChargeValidator : AbstractValidator<CreateAdHocChargeRequest>
{
    public CreateAdHocChargeValidator()
    {
        RuleFor(x => x.AcademicYearId)
            .NotEmpty()
            .WithMessage("Academic year is required.");

        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student is required.");

        RuleFor(x => x.ChargeTypeId)
            .NotEmpty()
            .WithMessage("Charge type is required.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters.");

        RuleFor(x => x.UnitAmount)
            .GreaterThanOrEqualTo(0m)
            .WithMessage("Unit amount cannot be negative.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}
