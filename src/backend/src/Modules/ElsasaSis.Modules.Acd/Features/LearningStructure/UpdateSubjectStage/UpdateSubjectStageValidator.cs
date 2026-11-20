using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubjectStage;

public sealed class UpdateSubjectStageValidator : AbstractValidator<UpdateSubjectStageRequest>
{
    public UpdateSubjectStageValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject stage name cannot be empty.")
            .MaximumLength(100).WithMessage("Subject stage name must not exceed 100 characters.");

        RuleFor(x => x.SequenceOrder)
            .GreaterThan(0).WithMessage("Sequence order must be greater than zero.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
