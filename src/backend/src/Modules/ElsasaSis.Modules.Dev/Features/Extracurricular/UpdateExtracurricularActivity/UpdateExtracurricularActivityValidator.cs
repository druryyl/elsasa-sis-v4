using ElsasaSis.Modules.Dev.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.UpdateExtracurricularActivity;

public sealed class UpdateExtracurricularActivityValidator : AbstractValidator<UpdateExtracurricularActivityRequest>
{
    public UpdateExtracurricularActivityValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Activity name is required.")
            .MaximumLength(100).WithMessage("Activity name cannot exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.SupervisorId)
            .NotEmpty().WithMessage("Supervisor identifier is required.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(ExtracurricularActivityStatus.IsValid)
            .WithMessage("Status must be ACTIVE or INACTIVE.");
    }
}
