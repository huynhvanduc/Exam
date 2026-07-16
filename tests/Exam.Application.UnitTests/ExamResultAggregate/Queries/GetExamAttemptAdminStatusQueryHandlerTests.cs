using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.GetExamAttemptAdminStatus;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamAttemptAdminStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new GetExamAttemptAdminStatusQueryHandler(examResultRepository.Object, new Mock<IQuestionRepository>().Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetExamAttemptAdminStatusQuery("result-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredInProgress_AutoFinishesAndReturnsResultRegardlessOfOwner()
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
        var handler = new GetExamAttemptAdminStatusQueryHandler(examResultRepository.Object, questionRepository.Object, gradingService);

        var result = await handler.Handle(new GetExamAttemptAdminStatusQuery(examResult.Id), CancellationToken.None);

        Assert.True(result.Finished);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFinished_ReturnsAttemptDtoForAnyAdmin()
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
        var handler = new GetExamAttemptAdminStatusQueryHandler(examResultRepository.Object, questionRepository.Object, gradingService);

        var result = await handler.Handle(new GetExamAttemptAdminStatusQuery(examResult.Id), CancellationToken.None);

        Assert.False(result.Finished);
        Assert.NotNull(result.Attempt);
    }
}
