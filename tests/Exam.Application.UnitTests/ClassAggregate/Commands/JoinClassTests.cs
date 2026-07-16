using Exam.Application.ClassAggregate.Commands.JoinClass;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class JoinClassCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidJoinCode_AddsMemberAndReturnsDto()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByJoinCodeAsync(classRoom.JoinCode, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new JoinClassCommandHandler(repository.Object);

        var result = await handler.Handle(new JoinClassCommand(classRoom.JoinCode, "student-1"), CancellationToken.None);

        Assert.Equal(1, result.MemberCount);
        repository.Verify(r => r.UpdateAsync(classRoom, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_JoinCodeNormalizedToUpperTrimmed()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByJoinCodeAsync(classRoom.JoinCode, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new JoinClassCommandHandler(repository.Object);

        await handler.Handle(new JoinClassCommand($"  {classRoom.JoinCode.ToLowerInvariant()}  ", "student-1"), CancellationToken.None);

        repository.Verify(r => r.GetByJoinCodeAsync(classRoom.JoinCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidJoinCode_ThrowsNotFoundException()
    {
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByJoinCodeAsync("BADCODE", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new JoinClassCommandHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new JoinClassCommand("BADCODE", "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_OwnerJoiningOwnClass_ThrowsDomainException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var repository = new Mock<IClassRoomRepository>();
        repository.Setup(r => r.GetByJoinCodeAsync(classRoom.JoinCode, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new JoinClassCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new JoinClassCommand(classRoom.JoinCode, "teacher-1"), CancellationToken.None));
    }
}

public class JoinClassCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("JoinClassCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string joinCode, string userId, bool isValid)
    {
        var validator = new JoinClassCommandValidator();

        var result = validator.Validate(new JoinClassCommand(joinCode, userId));

        Assert.Equal(isValid, result.IsValid);
    }
}
