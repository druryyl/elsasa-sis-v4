using ElsasaSis.Modules.Acd.Domain.Model;
using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.Attendance.RecordSubjectAttendance;

public sealed class RecordAttendanceValidator : AbstractValidator<RecordAttendanceRequest>
{
    public RecordAttendanceValidator()
    {
        RuleFor(x => x.SubjectId)
            .NotEmpty()
            .WithMessage("Subject identifier is required.");

        RuleFor(x => x.StudyGroupId)
            .NotEmpty()
            .WithMessage("Study group identifier is required.");

        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty()
            .WithMessage("Operational period identifier is required.");

        RuleFor(x => x.AttendanceDate)
            .NotEmpty()
            .WithMessage("Attendance date is required.");

        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("At least one attendance item must be provided.")
            .Must(items => items is { Count: > 0 })
            .WithMessage("At least one attendance item must be provided.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.StudentId)
                .NotEmpty()
                .WithMessage("Student identifier is required.");

            item.RuleFor(i => i.Status)
                .NotEmpty()
                .WithMessage("Attendance status is required.")
                .Must(status => AttendanceStatusExtensions.ParseAttendanceStatus(status).IsSuccess)
                .WithMessage("Attendance status must be one of: PRESENT, ABSENT, EXCUSED, SICK, LATE.");

            item.RuleFor(i => i.Notes)
                .MaximumLength(500)
                .WithMessage("Attendance notes cannot exceed 500 characters.");
        });
    }
}
