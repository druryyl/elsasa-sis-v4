using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Admissions.RecordAdmissionDecision;

public sealed class RecordAdmissionDecisionValidator : AbstractValidator<RecordAdmissionDecisionRequest>
{
    public RecordAdmissionDecisionValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty()
            .Must(IsValidDecision)
            .WithMessage("Decision must be ACCEPTED or REJECTED.");

        RuleFor(x => x.DecisionNotes)
            .MaximumLength(500)
            .When(x => x.DecisionNotes is not null);
    }

    private static bool IsValidDecision(string? decision) =>
        !string.IsNullOrWhiteSpace(decision) &&
        (string.Equals(decision.Trim(), "ACCEPTED", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(decision.Trim(), "REJECTED", StringComparison.OrdinalIgnoreCase));
}
