using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.GetExamAnalytics;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamAnalyticsQueryHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) =>
        new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
            ownerUserId, "cat-1", "Toán học", true, 5m).WithId("exam-1");

    private static ExamResult CreateFinishedResult(string userId, string questionId, bool answeredCorrectly, decimal minimumPassingScore = 5m)
    {
        var examResult = new ExamResult(userId, "exam-1");
        examResult.AssignQuestions([questionId]);
        var answer = new AnswerResult("a-1", "Đáp án A", answeredCorrectly, true);
        var questionResult = new QuestionResult(questionId, "Nội dung", QuestionType.SingleSelection, Level.Easy, [answer], "");
        examResult.AddQuestionResult(questionResult);
        examResult.Finish(minimumPassingScore);
        return examResult;
    }

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new GetExamAnalyticsQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetExamAnalyticsQuery("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new GetExamAnalyticsQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetExamAnalyticsQuery(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoFinishedAttempts_ReturnsZeroedAnalytics()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0L);
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 0, 0, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetExamAnalyticsQueryHandler(examRepository.Object, examResultRepository.Object);

        var result = await handler.Handle(new GetExamAnalyticsQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(0, result.TotalAttempts);
        Assert.Equal(0m, result.AverageScore);
        Assert.Equal(0m, result.PassRate);
        Assert.Empty(result.HardestQuestions);
    }

    [Fact]
    public async Task Handle_ValidRequest_ComputesAverageScorePassRateAndHistogram()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var results = new[]
        {
            CreateFinishedResult("student-1", "q-1", answeredCorrectly: true),
            CreateFinishedResult("student-2", "q-1", answeredCorrectly: false)
        };
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(2L);
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 0, 2, It.IsAny<CancellationToken>())).ReturnsAsync(results);
        var handler = new GetExamAnalyticsQueryHandler(examRepository.Object, examResultRepository.Object);

        var result = await handler.Handle(new GetExamAnalyticsQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(2, result.TotalAttempts);
        Assert.Equal(5m, result.AverageScore);
        Assert.Equal(50m, result.PassRate);
        Assert.Equal(1, result.ScoreHistogram.Single(b => b.RangeLabel == "0-2").Count);
        Assert.Equal(1, result.ScoreHistogram.Single(b => b.RangeLabel == "8-10").Count);
    }

    [Fact]
    public async Task Handle_HardestQuestions_ExcludesQuestionsBelowMinimumAnswerThreshold()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var results = new[]
        {
            // "q-hard": đủ ngưỡng 3 lượt trả lời, 2/3 sai -> phải xuất hiện trong hardestQuestions.
            CreateFinishedResult("student-1", "q-hard", answeredCorrectly: false),
            CreateFinishedResult("student-2", "q-hard", answeredCorrectly: false),
            CreateFinishedResult("student-3", "q-hard", answeredCorrectly: true),
            // "q-rare": chỉ có 2 lượt trả lời (dưới ngưỡng) -> phải bị loại dù toàn sai.
            CreateFinishedResult("student-4", "q-rare", answeredCorrectly: false),
            CreateFinishedResult("student-5", "q-rare", answeredCorrectly: false)
        };
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(5L);
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 0, 5, It.IsAny<CancellationToken>())).ReturnsAsync(results);
        var handler = new GetExamAnalyticsQueryHandler(examRepository.Object, examResultRepository.Object);

        var result = await handler.Handle(new GetExamAnalyticsQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Single(result.HardestQuestions);
        var hardest = result.HardestQuestions.Single();
        Assert.Equal("q-hard", hardest.QuestionId);
        Assert.Equal(3, hardest.TotalAnswered);
        Assert.Equal(2, hardest.IncorrectCount);
    }
}
