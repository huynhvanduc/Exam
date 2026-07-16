using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.QuestionAggregate.Commands.UpdateQuestion;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Commands;

public class UpdateQuestionCommandHandlerTests
{
    private static readonly IReadOnlyCollection<AnswerInput> ValidAnswers =
    [
        new AnswerInput("Đáp án A", true),
        new AnswerInput("Đáp án B", false)
    ];

    private static Question CreateQuestion(string ownerUserId) => new(null!, "Nội dung cũ", QuestionType.SingleSelection,
        Level.Easy, "cat-1", [new Answer(null!, "Đáp án A", true)], "", ownerUserId, "Toán học");

    [Fact]
    public async Task Handle_QuestionNotFound_ThrowsNotFoundException()
    {
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync("q-1", It.IsAny<CancellationToken>())).ReturnsAsync((Question)null!);
        var handler = new UpdateQuestionCommandHandler(questionRepository.Object, new Mock<ICategoryRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateQuestionCommand("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy,
                "cat-1", ValidAnswers, "", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var handler = new UpdateQuestionCommandHandler(questionRepository.Object, new Mock<ICategoryRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new UpdateQuestionCommand(question.Id, "Nội dung", QuestionType.SingleSelection, Level.Easy,
                "cat-1", ValidAnswers, "", new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-2", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new UpdateQuestionCommandHandler(questionRepository.Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateQuestionCommand(question.Id, "Nội dung", QuestionType.SingleSelection, Level.Easy,
                "cat-2", ValidAnswers, "", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesQuestionAndReturnsDto()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var newCategory = Category.Create("Vật lý", "vat-ly").WithId("cat-2");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(newCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(newCategory);
        var handler = new UpdateQuestionCommandHandler(questionRepository.Object, categoryRepository.Object);

        var result = await handler.Handle(new UpdateQuestionCommand(question.Id, "Nội dung mới", QuestionType.MultipleSelection,
            Level.Difficult, newCategory.Id, ValidAnswers, "Giải thích mới", new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Equal("Nội dung mới", result.Content);
        Assert.Equal(newCategory.Id, result.CategoryId);
        Assert.Equal(newCategory.Name, result.CategoryName);
        Assert.Equal(2, result.Answers.Count);
        questionRepository.Verify(r => r.UpdateAsync(question, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class UpdateQuestionCommandValidatorTests
{
    private static readonly IReadOnlyCollection<AnswerInput> ValidAnswers = [new AnswerInput("Đáp án A", true)];

    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UpdateQuestionCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, string content, string categoryId, string explain, bool isValid)
    {
        var validator = new UpdateQuestionCommandValidator();

        var result = validator.Validate(new UpdateQuestionCommand(id, content, QuestionType.SingleSelection, Level.Easy,
            categoryId, ValidAnswers, explain, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }

    [Fact]
    public void Validate_NoCorrectAnswer_IsInvalid()
    {
        var validator = new UpdateQuestionCommandValidator();

        var result = validator.Validate(new UpdateQuestionCommand("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [new AnswerInput("Đáp án A", false)], "", new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_SingleSelectionWithTwoCorrectAnswers_IsInvalid()
    {
        var validator = new UpdateQuestionCommandValidator();

        var result = validator.Validate(new UpdateQuestionCommand("q-1", "Nội dung", QuestionType.SingleSelection, Level.Easy,
            "cat-1", [new AnswerInput("Đáp án A", true), new AnswerInput("Đáp án B", true)], "",
            new Actor("teacher-1", UserRole.Instructor)));

        Assert.False(result.IsValid);
    }
}
