using Exam.Application.Exceptions;
using Exam.Application.QuestionAggregate.Commands.CreateQuestion;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Commands;

public class CreateQuestionCommandHandlerTests
{
    private static readonly IReadOnlyCollection<AnswerInput> ValidAnswers =
    [
        new AnswerInput("Đáp án A", true),
        new AnswerInput("Đáp án B", false)
    ];

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new CreateQuestionCommandHandler(new Mock<IQuestionRepository>().Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateQuestionCommand("Nội dung", QuestionType.SingleSelection, Level.Easy, "cat-1",
                ValidAnswers, "", "teacher-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_InsertsQuestionWithCategoryNameAndReturnsDto()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var questionRepository = new Mock<IQuestionRepository>();
        var handler = new CreateQuestionCommandHandler(questionRepository.Object, categoryRepository.Object);

        var result = await handler.Handle(new CreateQuestionCommand("Nội dung câu hỏi", QuestionType.SingleSelection,
            Level.Easy, category.Id, ValidAnswers, "Giải thích", "teacher-1"), CancellationToken.None);

        Assert.Equal("Nội dung câu hỏi", result.Content);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal(category.Name, result.CategoryName);
        Assert.Equal("teacher-1", result.OwnerUserId);
        Assert.Equal(2, result.Answers.Count);
        questionRepository.Verify(r => r.InsertAsync(
            It.Is<Question>(q => q.CategoryName == category.Name && q.OwnerUserId == "teacher-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CreateQuestionCommandValidatorTests
{
    private static readonly IReadOnlyCollection<AnswerInput> ValidAnswers = [new AnswerInput("Đáp án A", true)];

    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("CreateQuestionCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string content, string categoryId, string explain, bool isValid)
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand(content, QuestionType.SingleSelection, Level.Easy,
            categoryId, ValidAnswers, explain, "teacher-1"));

        Assert.Equal(isValid, result.IsValid);
    }

    [Fact]
    public void Validate_ContentExceeds2000Characters_IsInvalid()
    {
        var validator = new CreateQuestionCommandValidator();
        var longContent = new string('a', 2001);

        var result = validator.Validate(new CreateQuestionCommand(longContent, QuestionType.SingleSelection, Level.Easy,
            "cat-1", ValidAnswers, "", "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAnswers_IsInvalid()
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand("Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [], "", "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_AnswerContentEmpty_IsInvalid()
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand("Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [new AnswerInput("", true)], "", "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NoCorrectAnswer_IsInvalid()
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand("Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [new AnswerInput("Đáp án A", false)], "", "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_SingleSelectionWithTwoCorrectAnswers_IsInvalid()
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand("Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [new AnswerInput("Đáp án A", true), new AnswerInput("Đáp án B", true)], "", "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleSelectionWithTwoCorrectAnswers_IsValid()
    {
        var validator = new CreateQuestionCommandValidator();

        var result = validator.Validate(new CreateQuestionCommand("Nội dung", QuestionType.MultipleSelection, Level.Easy,
            "cat-1", [new AnswerInput("Đáp án A", true), new AnswerInput("Đáp án B", true)], "", "teacher-1"));

        Assert.True(result.IsValid);
    }
}
