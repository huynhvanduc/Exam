using Exam.Application.QuestionAggregate.Queries.GetQuestionsByCategoryPaged;
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Queries;

public class GetQuestionsByCategoryPagedQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedResultUsingSkipTakeAndCount()
    {
        var questions = new[]
        {
            new Question(null!, "Nội dung 1", QuestionType.SingleSelection, Level.Easy, "cat-1",
                [new Answer(null!, "Đáp án A", true)], "", "teacher-1", "Toán học")
        };
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByCategoryAsync("cat-1", 20, 20, Level.Easy, QuestionType.SingleSelection,
            "abc", It.IsAny<CancellationToken>())).ReturnsAsync(questions);
        questionRepository.Setup(r => r.CountByCategoryAsync("cat-1", Level.Easy, QuestionType.SingleSelection, "abc",
            It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var handler = new GetQuestionsByCategoryPagedQueryHandler(questionRepository.Object);

        var result = await handler.Handle(new GetQuestionsByCategoryPagedQuery("cat-1", 2, 20, Level.Easy,
            QuestionType.SingleSelection, "abc"), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(1L, result.TotalCount);
    }
}

public class GetQuestionsByCategoryPagedQueryValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("GetQuestionsByCategoryPagedQueryValidator.csv",
        CsvTestData.Str, CsvTestData.Int, CsvTestData.Int, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string categoryId, int page, int pageSize, bool isValid)
    {
        var validator = new GetQuestionsByCategoryPagedQueryValidator();

        var result = validator.Validate(new GetQuestionsByCategoryPagedQuery(categoryId, page, pageSize));

        Assert.Equal(isValid, result.IsValid);
    }
}
