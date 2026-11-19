using FluentValidation;

namespace ElsasaSis.Modules.Tch.Features.Assignments.AssignTeacher;

public sealed class AssignTeacherValidator : AbstractValidator<AssignTeacherRequest>
{
    public AssignTeacherValidator()
    {
        RuleFor(x => x.TeacherId)
            .NotEmpty();

        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty();

        RuleFor(x => x.SubjectId)
            .NotEmpty();

        RuleFor(x => x.StudyGroupId)
            .NotEmpty();
    }
}
