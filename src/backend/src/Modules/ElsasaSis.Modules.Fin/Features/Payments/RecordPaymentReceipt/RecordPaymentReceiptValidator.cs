using ElsasaSis.Modules.Fin.Domain.Model;
using FluentValidation;

namespace ElsasaSis.Modules.Fin.Features.Payments.RecordPaymentReceipt;

public sealed class RecordPaymentReceiptValidator : AbstractValidator<RecordPaymentReceiptRequest>
{
    public RecordPaymentReceiptValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("Student ID is required.");

        RuleFor(x => x.AcademicYearId)
            .NotEmpty()
            .WithMessage("Academic year ID is required.");

        RuleFor(x => x.PaymentDate)
            .Must(d => d != default)
            .WithMessage("Payment date is required.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required.")
            .Must(m => PaymentMethods.IsValid(m?.Trim().ToUpperInvariant() ?? string.Empty))
            .WithMessage("Payment method must be CASH or BANK_TRANSFER.");

        RuleFor(x => x.TotalAmount)
            .GreaterThan(0m)
            .WithMessage("Total amount must be greater than zero.");

        RuleForEach(x => x.Allocations)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ChargeId)
                    .NotEmpty()
                    .WithMessage("Allocated charge ID cannot be empty.");

                item.RuleFor(i => i.Amount)
                    .GreaterThan(0m)
                    .WithMessage("Allocated amount must be greater than zero.");
            });

        RuleFor(x => x)
            .Must(x => x.Allocations == null || x.Allocations.Count == 0 || x.Allocations.Sum(a => a.Amount) <= x.TotalAmount)
            .WithMessage("Sum of allocated amounts cannot exceed total receipt amount.")
            .WithName("Allocations");
    }
}
