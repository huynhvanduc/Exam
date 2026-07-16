using Exam.Application.QuestionAggregate.Queries.GetQuestionById;
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Queries;

public class GetQuestionByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync("q-1", It.IsAny<CancellationToken>())).ReturnsAsync((Question)null!);
        var handler = new GetQuestionByIdQueryHandler(questionRepository.Object);

        var result = await handler.Handle(new GetQuestionByIdQuery("q-1"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_Found_ReturnsDto()
    {
        var question = new Question(null!, "Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
            [new Answer(null!, "Đáp án A", true)], "", "teacher-1", "Toán học");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var handler = new GetQuestionByIdQueryHandler(questionRepository.Object);

        var result = await handler.Handle(new GetQuestionByIdQuery(question.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Nội dung", result!.Content);
    }
}
