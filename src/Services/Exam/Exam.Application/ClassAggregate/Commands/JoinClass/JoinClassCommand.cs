using MediatR;

namespace Exam.Application.ClassAggregate.Commands.JoinClass;

public record JoinClassCommand(string JoinCode, string UserId) : IRequest<ClassRoomDto>;
