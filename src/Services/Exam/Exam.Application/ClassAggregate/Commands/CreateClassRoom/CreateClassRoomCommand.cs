using MediatR;

namespace Exam.Application.ClassAggregate.Commands.CreateClassRoom;

public record CreateClassRoomCommand(string Name, string OwnerUserId) : IRequest<ClassRoomDto>;
