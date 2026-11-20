using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.Participations.EnrollStudentsInSubjectStages;

public sealed class EnrollStudentsInSubjectStagesValidator : AbstractValidator<EnrollStudentsInSubjectStagesRequest>
{
    public EnrollStudentsInSubjectStagesValidator()
    {
        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty().WithMessage("Operational period identifier is required.");

        RuleFor(x => x.SubjectStageId)
            .NotEmpty().WithMessage("Subject stage identifier is required.");

        RuleFor(x => x.StudyGroupId)
            .NotEmpty().WithMessage("Study group identifier is required.");
    }
}
