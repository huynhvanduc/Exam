using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.CreateClassRoom;

public class CreateClassRoomCommandValidator : AbstractValidator<CreateClassRoomCommand>
{
    public CreateClassRoomCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
