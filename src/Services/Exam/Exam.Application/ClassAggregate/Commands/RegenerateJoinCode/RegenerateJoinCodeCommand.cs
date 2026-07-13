using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RegenerateJoinCode;

public record RegenerateJoinCodeCommand(string Id, Actor Actor) : IRequest<ClassRoomDto>;
