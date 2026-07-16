using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.DeleteExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class DeleteExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) => new("Đề 1", "", "Nội dung",
        TimeSpan.FromMinutes(30), Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new DeleteExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteExamCommand("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenExceptionAndDoesNotDelete()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new DeleteExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteExamCommand(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));

        examRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_DeletesExam()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new DeleteExamCommandHandler(examRepository.Object);

        await handler.Handle(new DeleteExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        examRepository.Verify(r => r.DeleteAsync(exam.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_DeletesExamEvenWhenNotOwner()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new DeleteExamCommandHandler(examRepository.Object);

        await handler.Handle(new DeleteExamCommand(exam.Id, new Actor("admin-1", UserRole.Admin)), CancellationToken.None);

        examRepository.Verify(r => r.DeleteAsync(exam.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("DeleteExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, bool isValid)
    {
        var validator = new DeleteExamCommandValidator();

        var result = validator.Validate(new DeleteExamCommand(id, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
