using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.ReportCards.ProvideSubjectReportCardNotes;

public sealed class ProvideSubjectNotesValidator : AbstractValidator<ProvideSubjectNotesRequest>
{
    public ProvideSubjectNotesValidator()
    {
        RuleFor(x => x.SubjectStageId)
            .NotEmpty()
            .WithMessage("Subject stage identifier is required.");

        RuleFor(x => x.TeacherNotes)
            .NotEmpty()
            .WithMessage("Teacher notes cannot be empty.")
            .MaximumLength(1000)
            .WithMessage("Teacher notes cannot exceed 1000 characters.");
    }
}
