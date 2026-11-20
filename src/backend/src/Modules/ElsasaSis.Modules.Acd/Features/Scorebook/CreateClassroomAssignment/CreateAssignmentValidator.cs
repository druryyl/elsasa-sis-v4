using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.CreateClassroomAssignment;

public sealed class CreateAssignmentValidator : AbstractValidator<CreateAssignmentRequest>
{
    public CreateAssignmentValidator()
    {
        RuleFor(x => x.SubjectStageId)
            .NotEmpty()
            .WithMessage("Subject stage ID is required.");

        RuleFor(x => x.SubjectId)
            .NotEmpty()
            .WithMessage("Subject ID is required.");

        RuleFor(x => x.StudyGroupId)
            .NotEmpty()
            .WithMessage("Study group ID is required.");

        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty()
            .WithMessage("Operational period ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(150)
            .WithMessage("Title cannot exceed 150 characters.");

        RuleFor(x => x.AssignmentType)
            .NotEmpty()
            .WithMessage("Assignment type is required.")
            .Must(t => !string.IsNullOrWhiteSpace(t) && (
                t.Equals("FORMATIVE", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("SUMMATIVE", StringComparison.OrdinalIgnoreCase) ||
                t.Equals("EXAM", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Assignment type must be FORMATIVE, SUMMATIVE, or EXAM.");

        RuleFor(x => x.MaxScore)
            .GreaterThan(0.00m)
            .WithMessage("Max score must be greater than 0.");

        RuleFor(x => x.Weight)
            .Must(w => !w.HasValue || (w.Value >= 0.00m && w.Value <= 100.00m))
            .WithMessage("Weight must be between 0.00 and 100.00.");

        RuleFor(x => x.DueDate)
            .Must((req, due) => !due.HasValue || due.Value >= req.AssignedDate)
            .WithMessage("Due date cannot be earlier than assigned date.");
    }
}
