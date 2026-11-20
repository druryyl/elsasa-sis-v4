using ElsasaSis.Modules.Dev.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Discipline;

public sealed class RecordDisciplineIncidentValidator : AbstractValidator<RecordDisciplineIncidentRequest>
{
    public RecordDisciplineIncidentValidator()
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

        RuleFor(x => x.IncidentDate)
            .NotEmpty()
            .WithMessage("IncidentDate is required.");

        RuleFor(x => x.ViolationType)
            .NotEmpty()
            .WithMessage("ViolationType is required.")
            .MaximumLength(50)
            .WithMessage("ViolationType cannot exceed 50 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters.");

        RuleFor(x => x.Sanction)
            .NotEmpty()
            .WithMessage("Sanction is required.")
            .Must(DisciplineSanction.IsAllowedForTeacher)
            .WithMessage("Classroom discipline sanctions are restricted to VERBAL_WARNING or WRITTEN_WARNING.");
    }
}
