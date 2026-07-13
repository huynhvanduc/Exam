using MediatR;

namespace Exam.Application.ClassAggregate.Commands.RemoveMember;

public record RemoveMemberCommand(string ClassId, string UserId, Actor Actor) : IRequest<ClassRoomDetailDto>;
