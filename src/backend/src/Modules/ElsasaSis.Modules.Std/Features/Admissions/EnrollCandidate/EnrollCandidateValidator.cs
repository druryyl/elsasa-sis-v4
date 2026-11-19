using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Admissions.EnrollCandidate;

public sealed class EnrollCandidateValidator : AbstractValidator<EnrollCandidateRequest>
{
    public EnrollCandidateValidator()
    {
        RuleFor(x => x.Nis)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(x => x.Nisn)
            .MaximumLength(20)
            .When(x => x.Nisn is not null);

        RuleFor(x => x.EnrollmentDate)
            .Must(d => !d.HasValue || d.Value != default)
            .WithMessage("Enrollment date is invalid.");
    }
}
