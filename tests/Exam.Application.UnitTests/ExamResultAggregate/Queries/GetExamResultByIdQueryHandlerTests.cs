using Exam.Application.ExamResultAggregate.Queries.GetExamResultById;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamResultByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync("result-1", It.IsAny<CancellationToken>())).ReturnsAsync((ExamResult)null!);
        var handler = new GetExamResultByIdQueryHandler(examResultRepository.Object);

        var result = await handler.Handle(new GetExamResultByIdQuery("result-1", "student-1"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.Finish(5m);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var handler = new GetExamResultByIdQueryHandler(examResultRepository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new GetExamResultByIdQuery(examResult.Id, "other-student"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotFinished_ReturnsNull()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var handler = new GetExamResultByIdQueryHandler(examResultRepository.Object);

        var result = await handler.Handle(new GetExamResultByIdQuery(examResult.Id, "student-1"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Finished_ReturnsDto()
    {
        var examResult = new ExamResult("student-1", "exam-1").WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.Finish(5m);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetByIdAsync(examResult.Id, It.IsAny<CancellationToken>())).ReturnsAsync(examResult);
        var handler = new GetExamResultByIdQueryHandler(examResultRepository.Object);

        var result = await handler.Handle(new GetExamResultByIdQuery(examResult.Id, "student-1"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(examResult.Id, result!.Id);
    }
}
