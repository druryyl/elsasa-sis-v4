using ElsasaSis.Modules.Dev.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Behaviour;

public sealed class RecordBehaviourNoteValidator : AbstractValidator<RecordBehaviourNoteRequest>
{
    public RecordBehaviourNoteValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty()
            .WithMessage("StudentId is required.");

        RuleFor(x => x.StudentName)
            .NotEmpty()
            .WithMessage("StudentName is required.")
            .MaximumLength(150)
            .WithMessage("StudentName cannot exceed 150 characters.");

        RuleFor(x => x.StudentNis)
            .NotEmpty()
            .WithMessage("StudentNis is required.")
            .MaximumLength(30)
            .WithMessage("StudentNis cannot exceed 30 characters.");

        RuleFor(x => x.BehaviourType)
            .NotEmpty()
            .WithMessage("BehaviourType is required.")
            .Must(BehaviourType.IsValid)
            .WithMessage("BehaviourType must be POSITIVE or NEGATIVE.");

        RuleFor(x => x.ObservedDate)
            .NotEmpty()
            .WithMessage("ObservedDate is required.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.");
    }
}
