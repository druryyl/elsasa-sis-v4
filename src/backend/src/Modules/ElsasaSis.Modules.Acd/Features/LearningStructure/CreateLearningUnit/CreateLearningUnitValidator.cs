using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.CreateLearningUnit;

public sealed class CreateLearningUnitValidator : AbstractValidator<CreateLearningUnitRequest>
{
    public CreateLearningUnitValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Learning unit name cannot be empty.")
            .MaximumLength(150).WithMessage("Learning unit name must not exceed 150 characters.");

        RuleFor(x => x.SequenceOrder)
            .GreaterThan(0).WithMessage("Sequence order must be greater than zero.");

        RuleFor(x => x.UnitType)
            .NotEmpty().WithMessage("Unit type cannot be empty.")
            .MaximumLength(30).WithMessage("Unit type must not exceed 30 characters.")
            .Must(Domain.Model.UnitType.IsValid)
            .WithMessage("Unit type must be one of: MODULE, PACE, LESSON, PRESENTATION, BOOK, OTHER.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
