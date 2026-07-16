using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Commands.AdminForceFinishExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Commands;

public class AdminForceFinishExamCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExamResultNotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new AdminForceFinishExamCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AdminForceFinishExamCommand("result-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ForcesFinishRegardlessOfOwnerAndReturnsDto()
    {
        var examResult = new ExamResult("student-1", "exam-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var gradingService = new ExamResultGradingService(examRepository.Object, questionRepository.Object);
        var handler = new AdminForceFinishExamCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new AdminForceFinishExamCommand(examResult.Id), CancellationToken.None);

        Assert.True(result.Finished);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyFinished_DoesNotReFinishOrChangeScore()
    {
        var examResult = new ExamResult("student-1", "exam-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.Finish(5m);
        var finishedAt = examResult.ExamFinishDate;
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new AdminForceFinishExamCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new AdminForceFinishExamCommand(examResult.Id), CancellationToken.None);

        Assert.Equal(finishedAt, result.ExamFinishDate);
    }
}
