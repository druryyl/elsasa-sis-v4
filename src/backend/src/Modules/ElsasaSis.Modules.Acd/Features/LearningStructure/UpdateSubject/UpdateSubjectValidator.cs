using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.LearningStructure.UpdateSubject;

public sealed class UpdateSubjectValidator : AbstractValidator<UpdateSubjectRequest>
{
    public UpdateSubjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Subject name cannot be empty.")
            .MaximumLength(100).WithMessage("Subject name must not exceed 100 characters.");

        RuleFor(x => x.Code)
            .MaximumLength(30).WithMessage("Subject code must not exceed 30 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Code));
    }
}
