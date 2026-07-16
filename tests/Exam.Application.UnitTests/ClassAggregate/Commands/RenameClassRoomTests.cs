using Exam.Application.ClassAggregate.Commands.RenameClassRoom;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class RenameClassRoomCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new RenameClassRoomCommandHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameClassRoomCommand("class-1", "Lớp mới", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new RenameClassRoomCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new RenameClassRoomCommand(classRoom.Id, "Lớp mới", new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Owner_RenamesClassRoom()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new RenameClassRoomCommandHandler(repository.Object);

        var result = await handler.Handle(new RenameClassRoomCommand(classRoom.Id, "Lớp 10A2", new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal("Lớp 10A2", result.Name);
        repository.Verify(r => r.UpdateAsync(classRoom, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RenameClassRoomCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("RenameClassRoomCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, string name, bool isValid)
    {
        var validator = new RenameClassRoomCommandValidator();

        var result = validator.Validate(new RenameClassRoomCommand(id, name, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
