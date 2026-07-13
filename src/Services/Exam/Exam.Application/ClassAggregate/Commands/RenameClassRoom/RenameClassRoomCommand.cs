using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RenameClassRoom;

public record RenameClassRoomCommand(string Id, string Name, Actor Actor) : IRequest<ClassRoomDto>;
