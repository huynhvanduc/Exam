using Exam.Application.ExamAggregate.Queries.GetExamById;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Queries;

public class GetExamByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new GetExamByIdQueryHandler(examRepository.Object);

        var result = await handler.Handle(new GetExamByIdQuery("exam-1"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Found_ReturnsDto()
    {
        var exam = new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30),
            Level.Easy, "teacher-1", "cat-1", "Toán học", true, 5m);
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new GetExamByIdQueryHandler(examRepository.Object);

        var result = await handler.Handle(new GetExamByIdQuery(exam.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Đề 1", result!.Name);
    }
}
