using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.UnpublishExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class UnpublishExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreatePublishedExam(string ownerUserId)
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);
        exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        exam.Publish();
        return exam;
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new UnpublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UnpublishExamCommand("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreatePublishedExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UnpublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UnpublishExamCommand(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExamNotPublished_ThrowsDomainException()
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UnpublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new UnpublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UnpublishesExamAndReturnsDto()
    {
        var exam = CreatePublishedExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new UnpublishExamCommandHandler(examRepository.Object);

        var result = await handler.Handle(new UnpublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(ExamStatus.Draft, result.Status);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UnpublishExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UnpublishExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, bool isValid)
    {
        var validator = new UnpublishExamCommandValidator();

        var result = validator.Validate(new UnpublishExamCommand(examId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
