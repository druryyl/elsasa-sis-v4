using ElsasaSis.Modules.Dev.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Dev.Features.Achievements;

public sealed class RecordAchievementValidator : AbstractValidator<RecordAchievementRequest>
{
    public RecordAchievementValidator()
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

        RuleFor(x => x.AchievementType)
            .NotEmpty()
            .WithMessage("AchievementType is required.")
            .Must(AchievementType.IsValid)
            .WithMessage("AchievementType must be SPORTS, ARTS, SCIENCE, COMMUNITY, or OTHER.");

        RuleFor(x => x.EventName)
            .NotEmpty()
            .WithMessage("EventName is required.")
            .MaximumLength(200)
            .WithMessage("EventName cannot exceed 200 characters.");

        RuleFor(x => x.RecognitionLevel)
            .NotEmpty()
            .WithMessage("RecognitionLevel is required.")
            .Must(RecognitionLevel.IsValid)
            .WithMessage("RecognitionLevel must be SCHOOL, CITY, PROVINCE, or NATIONAL.");

        RuleFor(x => x.AchievementDate)
            .NotEmpty()
            .WithMessage("AchievementDate is required.");

        When(x => !string.IsNullOrWhiteSpace(x.Description), () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description cannot exceed 500 characters.");
        });
    }
}
