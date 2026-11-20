using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.RecordExtracurricularSessionLog;

public sealed class RecordExtracurricularSessionLogValidator : AbstractValidator<RecordExtracurricularSessionLogRequest>
{
    public RecordExtracurricularSessionLogValidator()
    {
        RuleFor(x => x.SessionDate)
            .NotEmpty().WithMessage("Session date is required.");

        RuleFor(x => x.Log)
            .NotEmpty().WithMessage("Session log content cannot be empty.")
            .MaximumLength(1000).WithMessage("Session log cannot exceed 1000 characters.");
    }
}
