using Exam.Application.ClassAggregate.Queries.GetClassRoomById;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Queries;

public class GetClassRoomByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new GetClassRoomByIdQueryHandler(repository.Object, new Mock<IUserRepository>().Object);

        var result = await handler.Handle(new GetClassRoomByIdQuery("class-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsDetail()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetClassRoomByIdQueryHandler(repository.Object, userRepository.Object);

        var result = await handler.Handle(new GetClassRoomByIdQuery(classRoom.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_Member_ReturnsDetail()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        classRoom.AddMember("student-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetClassRoomByIdQueryHandler(repository.Object, userRepository.Object);

        var result = await handler.Handle(new GetClassRoomByIdQuery(classRoom.Id, new Actor("student-1", UserRole.Student)), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Handle_NonMemberNonOwnerNonAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new GetClassRoomByIdQueryHandler(repository.Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetClassRoomByIdQuery(classRoom.Id, new Actor("random-student", UserRole.Student)), CancellationToken.None));
    }
}
