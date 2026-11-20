using ElsasaSis.Modules.Opr.Domain;
using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Timetables.ManageSmkBlockRotations;

public sealed class CreateSmkBlockValidator : AbstractValidator<CreateSmkBlockRequest>
{
    public CreateSmkBlockValidator()
    {
        RuleFor(x => x.OperationalPeriodId)
            .NotEmpty()
            .WithMessage("Operational period ID is required.");

        RuleFor(x => x.ScheduleType)
            .NotEmpty()
            .WithMessage("Schedule type is required.")
            .Must(type => ScheduleTypeExtensions.ParseBlockScheduleType(type).IsSuccess)
            .WithMessage("Block schedule type must be GENERAL or VOCATIONAL.");

        RuleFor(x => x.EndDate)
            .Must((req, endDate) => endDate >= req.StartDate)
            .WithMessage("End date must be on or after start date.");
    }
}
