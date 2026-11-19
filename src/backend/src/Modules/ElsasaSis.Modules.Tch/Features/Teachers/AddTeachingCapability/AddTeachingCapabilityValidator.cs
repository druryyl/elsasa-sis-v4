using FluentValidation;

namespace ElsasaSis.Modules.Tch.Features.Teachers.AddTeachingCapability;

public sealed class AddTeachingCapabilityValidator : AbstractValidator<AddTeachingCapabilityRequest>
{
    public AddTeachingCapabilityValidator()
    {
        RuleFor(x => x.SubjectId)
            .NotEmpty();
    }
}
