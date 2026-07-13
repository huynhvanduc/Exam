using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.DeleteClassRoom;

public class DeleteClassRoomCommandValidator : AbstractValidator<DeleteClassRoomCommand>
{
    public DeleteClassRoomCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
