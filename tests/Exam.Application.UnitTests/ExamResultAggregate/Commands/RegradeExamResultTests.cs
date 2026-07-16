using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Commands.RegradeExamResult;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.ExamResultAggregate.Commands;

public class RegradeExamResultCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExamResultNotFound_ThrowsNotFoundException()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var gradingService = new ExamResultGradingService(new Mock<IExamRepository>().Object, new Mock<IQuestionRepository>().Object);
        var handler = new RegradeExamResultCommandHandler(examResultRepository.Object, gradingService);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RegradeExamResultCommand("result-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_RecomputesScoreFromCurrentQuestionDataAndKeepsFinishDate()
    {
        var examResult = new ExamResult("student-1", "exam-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.RecordAnswer("q-1", ["a-1"]);
        var originalQuestion = new Question("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a-1", "Đáp án A", true), new Answer("a-2", "Đáp án B", false)], "", "teacher-1", "Toán học");
        var examRepository = new Mock<IExamRepository>();
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "", TimeSpan.FromMinutes(30), Level.Easy,
            "teacher-1", "cat-1", "Toán học", true, 5m);
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([originalQuestion]);
        var gradingService = new ExamResultGradingService(examRepository.Object, questionRepository.Object);
        await gradingService.GradeAndFinishAsync(examResult, CancellationToken.None);
        Assert.True(examResult.Passed);
        var originalFinishDate = examResult.ExamFinishDate;

        // Giảng viên phát hiện đáp án đúng bị nhập sai -> sửa lại "a-2" mới là đáp án đúng.
        var correctedQuestion = new Question("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer("a-1", "Đáp án A", false), new Answer("a-2", "Đáp án B", true)], "", "teacher-1", "Toán học");
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([correctedQuestion]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var handler = new RegradeExamResultCommandHandler(examResultRepository.Object, gradingService);

        var result = await handler.Handle(new RegradeExamResultCommand(examResult.Id), CancellationToken.None);

        Assert.False(result.Passed);
        Assert.Equal(0m, result.TotalScore);
        Assert.Equal(originalFinishDate, examResult.ExamFinishDate);
        examResultRepository.Verify(r => r.UpdateAsync(examResult, It.IsAny<CancellationToken>()), Times.Once);
    }
}
