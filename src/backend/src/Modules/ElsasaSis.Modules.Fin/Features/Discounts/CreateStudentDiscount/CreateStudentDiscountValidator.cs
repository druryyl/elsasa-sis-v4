using ElsasaSis.Modules.Fin.Domain.Model;
using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Discounts.CreateStudentDiscount;

public sealed class CreateStudentDiscountValidator : AbstractValidator<CreateStudentDiscountRequest>
{
    public CreateStudentDiscountValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.ChargeTypeId)
            .NotEmpty().WithMessage("Charge type ID is required.");

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage("Discount type is required.")
            .Must(t => DiscountTypes.IsValid(t?.Trim().ToUpperInvariant() ?? string.Empty))
            .WithMessage("Discount type must be FIXED_AMOUNT or PERCENTAGE.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0m).WithMessage("Discount value must be greater than zero.");

        When(x => string.Equals(x.DiscountType?.Trim(), DiscountTypes.Percentage, StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.DiscountValue)
                .LessThanOrEqualTo(100m).WithMessage("Percentage discount cannot exceed 100%.");
        });

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Discount reason is required.")
            .MaximumLength(StudentDiscount.MaxReasonLength)
            .WithMessage($"Discount reason cannot exceed {StudentDiscount.MaxReasonLength} characters.");
    }
}
