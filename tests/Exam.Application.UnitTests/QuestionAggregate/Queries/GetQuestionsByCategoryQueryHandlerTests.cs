using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategory;
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Queries;

public class GetQuestionsByCategoryQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsQuestionsForCategory()
    {
        var questions = new[]
        {
            new Question(null!, "Nội dung 1", QuestionType.SingleSelection, Level.Easy, "cat-1",
                [new Answer(null!, "Đáp án A", true)], "", "teacher-1", "Toán học")
        };
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync(questions);
        var handler = new GetQuestionsByCategoryQueryHandler(questionRepository.Object);

        var result = await handler.Handle(new GetQuestionsByCategoryQuery("cat-1"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_NoQuestions_ReturnsEmptyList()
    {
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetQuestionsByCategoryQueryHandler(questionRepository.Object);

        var result = await handler.Handle(new GetQuestionsByCategoryQuery("cat-1"), CancellationToken.None);

        Assert.Empty(result);
    }
}
