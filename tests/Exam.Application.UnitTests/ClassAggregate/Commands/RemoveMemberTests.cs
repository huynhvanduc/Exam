using Exam.Application.ClassAggregate.Commands.RemoveMember;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class RemoveMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new RemoveMemberCommandHandler(classRoomRepository.Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RemoveMemberCommand("class-1", "student-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new RemoveMemberCommandHandler(classRoomRepository.Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new RemoveMemberCommand(classRoom.Id, "student-1", new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Owner_RemovesMemberAndReturnsRemainingDetail()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        classRoom.AddMember("student-1");
        classRoom.AddMember("student-2");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new User("student-2", "s2@example.com", "Trần", "Thị B")]);
        var handler = new RemoveMemberCommandHandler(classRoomRepository.Object, userRepository.Object);

        var result = await handler.Handle(new RemoveMemberCommand(classRoom.Id, "student-1", new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Single(result.Members);
        Assert.DoesNotContain(result.Members, m => m.UserId == "student-1");
        classRoomRepository.Verify(r => r.UpdateAsync(classRoom, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class RemoveMemberCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("RemoveMemberCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string classId, string userId, bool isValid)
    {
        var validator = new RemoveMemberCommandValidator();

        var result = validator.Validate(new RemoveMemberCommand(classId, userId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
