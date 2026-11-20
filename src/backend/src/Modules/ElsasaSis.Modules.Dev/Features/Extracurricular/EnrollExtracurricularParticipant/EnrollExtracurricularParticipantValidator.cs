using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Extracurricular.EnrollExtracurricularParticipant;

public sealed class EnrollExtracurricularParticipantValidator : AbstractValidator<EnrollExtracurricularParticipantRequest>
{
    public EnrollExtracurricularParticipantValidator()
    {
        RuleFor(x => x.StudentId)
            .NotEmpty().WithMessage("Student identifier is required.");

        RuleFor(x => x.JoinedDate)
            .NotEmpty().WithMessage("Joined date is required.");
    }
}
