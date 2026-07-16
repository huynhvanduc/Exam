using Exam.Application.ClassAggregate.Queries.GetMyClassRooms;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Queries;

public class GetMyClassRoomsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsClassRoomsForMember()
    {
        var classRooms = new[] { ClassRoom.Create("Lớp 10A1", "teacher-1") };
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByMemberAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync(classRooms);
        var handler = new GetMyClassRoomsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyClassRoomsQuery("student-1"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_NoClassRooms_ReturnsEmptyList()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByMemberAsync("student-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetMyClassRoomsQueryHandler(repository.Object);

        var result = await handler.Handle(new GetMyClassRoomsQuery("student-1"), CancellationToken.None);

        Assert.Empty(result);
    }
}
