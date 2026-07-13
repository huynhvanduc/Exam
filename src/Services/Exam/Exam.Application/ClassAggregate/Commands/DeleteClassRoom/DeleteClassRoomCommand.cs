using MediatR;

namespace Exam.Application.ClassAggregate.Commands.DeleteClassRoom;

public record DeleteClassRoomCommand(string Id, Actor Actor) : IRequest;
