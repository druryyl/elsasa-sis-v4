using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.Placements.PlaceStudent;

public sealed class PlaceStudentValidator : AbstractValidator<PlaceStudentRequest>
{
    public PlaceStudentValidator()
    {
        RuleFor(x => x.AcademicYearId)
            .NotEmpty().WithMessage("Academic year is required.");

        RuleFor(x => x.SchoolLevel)
            .NotEmpty().WithMessage("School level is required.")
            .Must(l => string.Equals(l, "SD", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMP", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMA", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMK", StringComparison.OrdinalIgnoreCase))
            .WithMessage("School level must be SD, SMP, SMA, or SMK.");

        RuleFor(x => x.Grade)
            .NotEmpty().WithMessage("Grade is required.")
            .MaximumLength(20).WithMessage("Grade must not exceed 20 characters.");
    }
}

public sealed class PlaceStudentFlatValidator : AbstractValidator<PlaceStudentFlatRequest>
{
    public PlaceStudentFlatValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student ID is required.");

        RuleFor(x => x.AcademicYearId)
            .NotEmpty().WithMessage("Academic year is required.");

        RuleFor(x => x.SchoolLevel)
            .NotEmpty().WithMessage("School level is required.")
            .Must(l => string.Equals(l, "SD", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMP", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMA", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(l, "SMK", StringComparison.OrdinalIgnoreCase))
            .WithMessage("School level must be SD, SMP, SMA, or SMK.");

        RuleFor(x => x.Grade)
            .NotEmpty().WithMessage("Grade is required.")
            .MaximumLength(20).WithMessage("Grade must not exceed 20 characters.");
    }
}
