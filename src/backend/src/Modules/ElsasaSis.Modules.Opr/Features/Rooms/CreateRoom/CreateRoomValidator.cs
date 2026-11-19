using FluentValidation;

namespace ElsasaSis.Modules.Opr.Features.Rooms.CreateRoom;

public sealed class CreateRoomValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomValidator()
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
