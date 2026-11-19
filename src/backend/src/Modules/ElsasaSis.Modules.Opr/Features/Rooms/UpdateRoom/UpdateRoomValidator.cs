using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Rooms.UpdateRoom;

public sealed class UpdateRoomValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RoomType)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Capacity)
            .GreaterThan(0);
    }
}
