using Exam.Application.ClassAggregate.Queries.GetAllClassRooms;
using Exam.Application.Common;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Queries;

public class GetAllClassRoomsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Admin_ReturnsAllClassRooms()
    {
        var classRooms = new[] { ClassRoom.Create("Lớp A", "teacher-1"), ClassRoom.Create("Lớp B", "teacher-2") };
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(classRooms);
        var handler = new GetAllClassRoomsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllClassRoomsQuery(new Actor("admin-1", UserRole.Admin)), CancellationToken.None);

        Assert.Equal(2, result.Count);
        repository.Verify(r => r.GetByOwnerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Instructor_ReturnsOnlyOwnClassRooms()
    {
        var classRooms = new[] { ClassRoom.Create("Lớp A", "teacher-1") };
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByOwnerAsync("teacher-1", It.IsAny<CancellationToken>())).ReturnsAsync(classRooms);
        var handler = new GetAllClassRoomsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetAllClassRoomsQuery(new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Single(result);
        repository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
