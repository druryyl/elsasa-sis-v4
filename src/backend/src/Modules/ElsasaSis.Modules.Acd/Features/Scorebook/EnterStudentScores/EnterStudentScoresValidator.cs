using FluentValidation;

namespace ElsasaSis.Modules.Acd.Features.Scorebook.EnterStudentScores;

public sealed class EnterStudentScoresValidator : AbstractValidator<EnterStudentScoresRequest>
{
    public EnterStudentScoresValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one score entry item is required.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.StudentId)
                    .NotEmpty()
                    .WithMessage("Student ID is required.");

                item.RuleFor(i => i.Score)
                    .GreaterThanOrEqualTo(0.00m)
                    .WithMessage("Score cannot be negative.");

                item.When(i => i.Feedback is not null, () =>
                {
                    item.RuleFor(i => i.Feedback!)
                        .MaximumLength(1000)
                        .WithMessage("Feedback cannot exceed 1000 characters.");
                });
            });
    }
}
