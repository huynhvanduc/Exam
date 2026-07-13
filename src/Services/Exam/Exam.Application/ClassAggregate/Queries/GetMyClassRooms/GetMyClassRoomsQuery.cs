using MediatR;

namespace Exam.Application.ClassAggregate.Queries.GetMyClassRooms;

public record GetMyClassRoomsQuery(string UserId) : IRequest<IReadOnlyCollection<ClassRoomDto>>;
