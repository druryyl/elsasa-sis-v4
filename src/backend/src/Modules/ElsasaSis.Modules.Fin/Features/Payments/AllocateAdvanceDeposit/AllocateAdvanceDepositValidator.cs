using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Payments.AllocateAdvanceDeposit;

public sealed class AllocateAdvanceDepositValidator : AbstractValidator<AllocateAdvanceDepositRequest>
{
    public AllocateAdvanceDepositValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.SourceReceiptId)
            .NotEmpty()
            .WithMessage("Source receipt ID is required.");

        RuleFor(x => x.Allocations)
            .NotNull()
            .NotEmpty()
            .WithMessage("At least one charge allocation is required.");

        RuleForEach(x => x.Allocations)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ChargeId)
                    .NotEmpty()
                    .WithMessage("Charge ID is required.");

                item.RuleFor(i => i.Amount)
                    .GreaterThan(0m)
                    .WithMessage("Allocation amount must be greater than zero.");
            });
    }
}
