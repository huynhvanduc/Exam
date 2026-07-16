using Exam.Application.ClassAggregate.Commands.RegenerateJoinCode;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class RegenerateJoinCodeCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new RegenerateJoinCodeCommandHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RegenerateJoinCodeCommand("class-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new RegenerateJoinCodeCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new RegenerateJoinCodeCommand(classRoom.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Owner_RegeneratesJoinCode()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var originalCode = classRoom.JoinCode;
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new RegenerateJoinCodeCommandHandler(repository.Object);

        var result = await handler.Handle(new RegenerateJoinCodeCommand(classRoom.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(6, result.JoinCode.Length);
        repository.Verify(r => r.UpdateAsync(classRoom, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RegenerateJoinCodeCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("RegenerateJoinCodeCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, bool isValid)
    {
        var validator = new RegenerateJoinCodeCommandValidator();

        var result = validator.Validate(new RegenerateJoinCodeCommand(id, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
