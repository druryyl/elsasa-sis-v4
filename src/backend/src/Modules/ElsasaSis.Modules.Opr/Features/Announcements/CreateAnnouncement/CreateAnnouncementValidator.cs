using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Announcements.CreateAnnouncement;

public sealed class CreateAnnouncementValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementValidator()
    {
        RuleFor(x => x.AcademicYearId)
            .Must(id => id is null || id.Value != Guid.Empty)
            .WithMessage("Academic year ID cannot be empty.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Content)
            .NotEmpty();

        RuleFor(x => x.TargetAudience)
            .NotEmpty()
            .MaximumLength(20);
    }
}
