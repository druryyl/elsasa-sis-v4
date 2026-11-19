using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Students.UpdateStudent;

public sealed class UpdateStudentValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must not exceed 150 characters.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(d => d != default && d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth must be a valid date not in the future.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.")
            .Must(g => string.Equals(g, "MALE", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(g, "FEMALE", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Gender must be MALE or FEMALE.");

        RuleFor(x => x.Nisn)
            .MaximumLength(20).WithMessage("National student number (NISN) must not exceed 20 characters.");

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters.")
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email is not a valid email address.");

        RuleFor(x => x.Nickname)
            .MaximumLength(50).WithMessage("Nickname must not exceed 50 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");
    }
}
