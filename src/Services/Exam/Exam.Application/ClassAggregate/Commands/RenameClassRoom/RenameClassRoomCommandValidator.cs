using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.RenameClassRoom;

public class RenameClassRoomCommandValidator : AbstractValidator<RenameClassRoomCommand>
{
    public RenameClassRoomCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
