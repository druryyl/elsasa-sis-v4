using FluentValidation;

namespace ElsasaSis.Modules.Std.Features.StudyGroups.UpdateStudyGroup;

public sealed class UpdateStudyGroupValidator : AbstractValidator<UpdateStudyGroupRequest>
{
    public UpdateStudyGroupValidator()
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

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Study group name is required.")
            .MaximumLength(50).WithMessage("Study group name must not exceed 50 characters.");
    }
}
