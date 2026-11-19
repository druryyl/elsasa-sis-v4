using FluentValidation;

namespace ElsasaSis.Modules.Tch.Features.Teachers;

public interface ITeacherProfileRequest
{
    string FullName { get; }
    string? Nickname { get; }
    string? Nip { get; }
    string? Nik { get; }
    DateOnly DateOfBirth { get; }
    string Gender { get; }
    string? Phone { get; }
    string? Email { get; }
    string? Address { get; }
}

public abstract class TeacherProfileValidatorBase<T> : AbstractValidator<T>
    where T : ITeacherProfileRequest
{
    protected TeacherProfileValidatorBase()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Nickname)
            .MaximumLength(50)
            .When(x => x.Nickname is not null);

        RuleFor(x => x.Nip)
            .MaximumLength(30)
            .When(x => x.Nip is not null);

        RuleFor(x => x.Nik)
            .MaximumLength(30)
            .When(x => x.Nik is not null);

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Teacher date of birth cannot be in the future.");

        RuleFor(x => x.Gender)
            .NotEmpty()
            .MaximumLength(10)
            .Must(IsValidGender)
            .WithMessage("Teacher gender must be MALE or FEMALE.");

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .MaximumLength(150)
            .When(x => x.Email is not null);

        RuleFor(x => x.Address)
            .MaximumLength(500)
            .When(x => x.Address is not null);

        // BR-TCH-002: A Teacher Profile must have at least one contact method (phone or email).
        RuleFor(x => x)
            .Custom((request, context) =>
            {
                if (string.IsNullOrWhiteSpace(request.Phone) && string.IsNullOrWhiteSpace(request.Email))
                {
                    const string message = "At least one contact method (phone or email) is required.";
                    context.AddFailure(nameof(ITeacherProfileRequest.Phone), message);
                    context.AddFailure(nameof(ITeacherProfileRequest.Email), message);
                }
            });
    }

    private static bool IsValidGender(string? gender) =>
        !string.IsNullOrWhiteSpace(gender) &&
        (string.Equals(gender.Trim(), "MALE", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(gender.Trim(), "FEMALE", StringComparison.OrdinalIgnoreCase));
}
