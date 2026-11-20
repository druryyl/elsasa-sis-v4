using ElsasaSis.Modules.Fin.Domain.Model;
using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.FeePolicy.UpdateChargeType;

public sealed class UpdateChargeTypeValidator : AbstractValidator<UpdateChargeTypeRequest>
{
    public UpdateChargeTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Charge type name is required.")
            .MaximumLength(ChargeType.MaxNameLength).WithMessage($"Charge type name cannot exceed {ChargeType.MaxNameLength} characters.");

        RuleFor(x => x.Frequency)
            .NotEmpty().WithMessage("Frequency is required.")
            .Must(f => ChargeFrequencies.IsValid(f?.Trim().ToUpperInvariant() ?? string.Empty))
            .WithMessage("Frequency must be one of: MONTHLY, SEMESTER, YEARLY, ONE_TIME.");

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0m).WithMessage("Amount must be greater than or equal to 0.");
    }
}
