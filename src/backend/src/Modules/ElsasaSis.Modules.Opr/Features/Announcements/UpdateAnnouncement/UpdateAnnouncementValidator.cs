using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Announcements.UpdateAnnouncement;

public sealed class UpdateAnnouncementValidator : AbstractValidator<UpdateAnnouncementRequest>
{
    public UpdateAnnouncementValidator()
    {
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
