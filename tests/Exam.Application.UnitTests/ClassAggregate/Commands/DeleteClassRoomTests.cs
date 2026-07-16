using Exam.Application.ClassAggregate.Commands.DeleteClassRoom;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class DeleteClassRoomCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new DeleteClassRoomCommandHandler(repository.Object);

        await Assert.ThrowsAsync<Exam.Application.Exceptions.NotFoundException>(() =>
            handler.Handle(new DeleteClassRoomCommand("class-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new DeleteClassRoomCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteClassRoomCommand(classRoom.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Owner_DeletesClassRoom()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new DeleteClassRoomCommandHandler(repository.Object);

        await handler.Handle(new DeleteClassRoomCommand(classRoom.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        repository.Verify(r => r.DeleteAsync(classRoom.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteClassRoomCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("DeleteClassRoomCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, bool isValid)
    {
        var validator = new DeleteClassRoomCommandValidator();

        var result = validator.Validate(new DeleteClassRoomCommand(id, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
