using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.UnassignExamFromClass;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class UnassignExamFromClassCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExamAssignedToClass(string ownerUserId, string classId)
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);
        exam.AssignToClass(classId);
        return exam;
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new UnassignExamFromClassCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UnassignExamFromClassCommand("exam-1", "class-1", new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExamAssignedToClass("teacher-1", "class-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UnassignExamFromClassCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UnassignExamFromClassCommand(exam.Id, "class-1", new Actor("other-teacher", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UnassignsClassAndReturnsDto()
    {
        var exam = CreateExamAssignedToClass("teacher-1", "class-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UnassignExamFromClassCommandHandler(examRepository.Object);

        var result = await handler.Handle(new UnassignExamFromClassCommand(exam.Id, "class-1", new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.DoesNotContain("class-1", result.AssignedClassIds);
        Assert.True(result.IsPublic);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UnassignExamFromClassCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UnassignExamFromClassCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, string classId, bool isValid)
    {
        var validator = new UnassignExamFromClassCommandValidator();

        var result = validator.Validate(new UnassignExamFromClassCommand(examId, classId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
