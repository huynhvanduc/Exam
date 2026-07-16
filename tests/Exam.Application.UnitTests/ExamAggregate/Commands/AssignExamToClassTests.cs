using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.AssignExamToClass;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class AssignExamToClassCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new AssignExamToClassCommandHandler(examRepository.Object, new Mock<IClassRoomRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AssignExamToClassCommand("exam-1", "class-1", new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdminOfExam_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new AssignExamToClassCommandHandler(examRepository.Object, new Mock<IClassRoomRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new AssignExamToClassCommand(exam.Id, "class-1", new Actor("other-teacher", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new AssignExamToClassCommandHandler(examRepository.Object, classRoomRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AssignExamToClassCommand(exam.Id, "class-1", new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdminOfClass_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var classRoom = ClassRoom.Create("Lớp 10A1", "other-teacher");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new AssignExamToClassCommandHandler(examRepository.Object, classRoomRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new AssignExamToClassCommand(exam.Id, classRoom.Id, new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_AssignsClassAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1").WithId("class-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new AssignExamToClassCommandHandler(examRepository.Object, classRoomRepository.Object);

        var result = await handler.Handle(new AssignExamToClassCommand(exam.Id, classRoom.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Contains(classRoom.Id, result.AssignedClassIds);
        Assert.False(result.IsPublic);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AssignExamToClassCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("AssignExamToClassCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, string classId, bool isValid)
    {
        var validator = new AssignExamToClassCommandValidator();

        var result = validator.Validate(new AssignExamToClassCommand(examId, classId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
