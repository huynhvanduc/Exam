using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetAllClassRooms;

public record GetAllClassRoomsQuery(Actor Actor) : IRequest<IReadOnlyCollection<ClassRoomDto>>;
