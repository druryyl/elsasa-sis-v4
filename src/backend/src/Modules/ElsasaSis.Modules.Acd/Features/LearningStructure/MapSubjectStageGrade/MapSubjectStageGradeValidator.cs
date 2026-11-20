using ElsasaSis.Modules.Acd.Domain.Model;
using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.MapSubjectStageGrade;

public sealed class MapSubjectStageGradeValidator : AbstractValidator<MapSubjectStageGradeRequest>
{
    public MapSubjectStageGradeValidator()
    {
        RuleFor(x => x.SchoolLevel)
            .NotEmpty().WithMessage("School level cannot be empty.")
            .Must(SchoolLevel.IsValid)
            .WithMessage("School level must be one of: SD, SMP, SMA, SMK.");

        RuleFor(x => x.Grade)
            .NotEmpty().WithMessage("Grade cannot be empty.")
            .MaximumLength(10).WithMessage("Grade must not exceed 10 characters.");

        RuleFor(x => x)
            .Must(x => SchoolLevel.IsValidCombination(x.SchoolLevel, x.Grade))
            .WithMessage(x => $"Invalid grade '{x.Grade}' for school level '{x.SchoolLevel}'.")
            .When(x => !string.IsNullOrWhiteSpace(x.SchoolLevel) && !string.IsNullOrWhiteSpace(x.Grade));
    }
}
