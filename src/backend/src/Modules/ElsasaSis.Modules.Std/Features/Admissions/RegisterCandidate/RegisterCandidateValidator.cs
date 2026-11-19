using ElsasaSis.Modules.Std.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Admissions.RegisterCandidate;

public sealed class RegisterCandidateValidator : AbstractValidator<RegisterCandidateRequest>
{
    public RegisterCandidateValidator()
    {
        RuleFor(x => x.AdmissionYearId)
            .NotEmpty();

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Nickname)
            .MaximumLength(50)
            .When(x => x.Nickname is not null);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Candidate date of birth cannot be in the future.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .MaximumLength(10)
            .Must(g => StdDomainParsing.ParseGender(g).IsSuccess)
            .WithMessage("Gender must be MALE or FEMALE.");

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .When(x => x.Email is not null);

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => x.Address is not null);

        RuleFor(x => x.TargetSchoolLevel)
            .NotEmpty()
            .MaximumLength(10)
            .Must(l => StdDomainParsing.ParseSchoolLevel(l).IsSuccess)
            .WithMessage("Target school level must be SD, SMP, SMA, or SMK.");

        RuleFor(x => x.TargetGrade)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Guardians)
            .NotEmpty()
            .WithMessage("At least one guardian is required.");

        RuleForEach(x => x.Guardians)
            .NotNull()
            .ChildRules(guardian =>
            {
                guardian.RuleFor(g => g.FullName)
                    .NotEmpty()
                    .MaximumLength(150);

                guardian.RuleFor(g => g.RelationshipType)
                    .NotEmpty()
                    .MaximumLength(20)
                    .Must(r => StdDomainParsing.ParseRelationshipType(r).IsSuccess)
                    .WithMessage("Guardian relationship type must be FATHER, MOTHER, or GUARDIAN.");

                guardian.RuleFor(g => g.Phone)
                    .MaximumLength(30)
                    .When(g => g.Phone is not null);

                guardian.RuleFor(g => g.Email)
                    .MaximumLength(150)
                    .When(g => g.Email is not null);

                guardian.RuleFor(g => g.Address)
                    .MaximumLength(500)
                    .When(g => g.Address is not null);
            });
    }
}
