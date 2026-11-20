using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.CreateExtracurricularActivity;

public sealed class CreateExtracurricularActivityValidator : AbstractValidator<CreateExtracurricularActivityRequest>
{
    public CreateExtracurricularActivityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Activity name is required.")
            .MaximumLength(100).WithMessage("Activity name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.SupervisorId)
            .NotEmpty().WithMessage("Supervisor identifier is required.");
    }
}
