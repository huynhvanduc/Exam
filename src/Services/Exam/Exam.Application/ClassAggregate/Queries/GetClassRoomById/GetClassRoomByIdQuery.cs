using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetClassRoomById;

public record GetClassRoomByIdQuery(string Id, Actor Actor) : IRequest<ClassRoomDetailDto?>;
