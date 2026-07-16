using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.GetExamAttempt;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamAttemptQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new GetExamAttemptQueryHandler(examResultRepository.Object, new Mock<IQuestionRepository>().Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetExamAttemptQuery("result-1", "student-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new GetExamAttemptQueryHandler(examResultRepository.Object, new Mock<IQuestionRepository>().Object, gradingService);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new GetExamAttemptQuery(examResult.Id, "other-student"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredInProgress_AutoFinishesAndReturnsResult()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.SetDuration(TimeSpan.FromMinutes(-1));
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var gradingService = new ExamResultGradingService(examRepository.Object, questionRepository.Object);
        var handler = new GetExamAttemptQueryHandler(examResultRepository.Object, questionRepository.Object, gradingService);

        var result = await handler.Handle(new GetExamAttemptQuery(examResult.Id, "student-1"), CancellationToken.None);

        Assert.True(result.Finished);
        Assert.NotNull(result.Result);
        Assert.Null(result.Attempt);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFinished_ReturnsAttemptDto()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var question = new Question("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a-1", "Đáp án A", true)], "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([question]);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, questionRepository.Object);
        var handler = new GetExamAttemptQueryHandler(examResultRepository.Object, questionRepository.Object, gradingService);

        var result = await handler.Handle(new GetExamAttemptQuery(examResult.Id, "student-1"), CancellationToken.None);

        Assert.False(result.Finished);
        Assert.NotNull(result.Attempt);
        Assert.Null(result.Result);
        examResultRepository.Verify(r => r.UpdateAsync(It.IsAny<ExamResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyFinished_ReturnsResultDtoWithoutQuerying()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.Finish(5m);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new GetExamAttemptQueryHandler(examResultRepository.Object, new Mock<IQuestionRepository>().Object, gradingService);

        var result = await handler.Handle(new GetExamAttemptQuery(examResult.Id, "student-1"), CancellationToken.None);

        Assert.True(result.Finished);
        Assert.NotNull(result.Result);
        examResultRepository.Verify(r => r.UpdateAsync(It.IsAny<ExamResult>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
