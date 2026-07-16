using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Commands.FinishExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Commands;

public class FinishExamCommandHandlerTests
{
    private static ExamResult CreateInProgressResult(string userId)
    {
        var examResult = new ExamResult(userId, "exam-1");
        examResult.AssignQuestions(["q-1"]);
        return examResult;
    }

    [Fact]
    public async Task Handle_ExamResultNotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new FinishExamCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new FinishExamCommand("result-1", "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var examResult = CreateInProgressResult("student-1");
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new FinishExamCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new FinishExamCommand(examResult.Id, "other-student"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_GradesAndFinishesExamReturningDto()
    {
        var examResult = CreateInProgressResult("student-1");
        examResult.RecordAnswer("q-1", ["a-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var question = new Question("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a-1", "Đáp án A", true)], "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        var gradingService = new ExamResultGradingService(examRepository.Object, questionRepository.Object);
        var handler = new FinishExamCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new FinishExamCommand(examResult.Id, "student-1"), CancellationToken.None);

        Assert.True(result.Finished);
        Assert.Equal(10m, result.TotalScore);
        Assert.True(result.Passed);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class FinishExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("FinishExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string examResultId, string userId, bool isValid)
    {
        var validator = new FinishExamCommandValidator();

        var result = validator.Validate(new FinishExamCommand(examResultId, userId));

        Assert.Equal(isValid, result.IsValid);
    }
}
