using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.PublishExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class PublishExamCommandHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId, bool withComposition = true)
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, ownerUserId, "cat-1", "Toán học", true, 5m);
        if (withComposition)
            exam.ConfigureComposition([new ExamCompositionCell(Level.Easy, QuestionType.SingleSelection, 5)]);
        return exam;
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new PublishExamCommand("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new PublishExamCommand(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoQuestions_ThrowsDomainExceptionAndDoesNotUpdate()
    {
        var exam = CreateExam("teacher-1", withComposition: false);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new PublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));

        examRepository.Verify(r => r.UpdateAsync(It.IsAny<Domain.AggregateModels.ExamAggregate.Exam>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyPublished_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.Publish();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new PublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Archived_ThrowsDomainException()
    {
        var exam = CreateExam("teacher-1");
        exam.Archive();
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new PublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_PublishesExamAndReturnsDto()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new PublishExamCommandHandler(examRepository.Object);

        var result = await handler.Handle(new PublishExamCommand(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(ExamStatus.Published, result.Status);
        examRepository.Verify(r => r.UpdateAsync(exam, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class PublishExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("PublishExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examId, bool isValid)
    {
        var validator = new PublishExamCommandValidator();

        var result = validator.Validate(new PublishExamCommand(examId, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
