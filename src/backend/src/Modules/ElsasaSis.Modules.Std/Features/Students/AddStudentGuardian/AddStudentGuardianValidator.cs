using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Students.AddStudentGuardian;

public sealed class AddStudentGuardianValidator : AbstractValidator<AddStudentGuardianRequest>
{
    public AddStudentGuardianValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Guardian full name is required.")
            .MaximumLength(150).WithMessage("Guardian full name must not exceed 150 characters.");

        RuleFor(x => x.RelationshipType)
            .NotEmpty().WithMessage("Relationship type is required.")
            .Must(r => string.Equals(r, "FATHER", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(r, "MOTHER", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(r, "GUARDIAN", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Relationship type must be FATHER, MOTHER, or GUARDIAN.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}
