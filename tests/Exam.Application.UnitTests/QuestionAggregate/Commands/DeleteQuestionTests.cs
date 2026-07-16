using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.QuestionAggregate.Commands.DeleteQuestion;
using Exam.Contracts;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Commands;

public class DeleteQuestionCommandHandlerTests
{
    private static Question CreateQuestion(string ownerUserId) => new(null!, "Nội dung", QuestionType.SingleSelection,
        Level.Easy, "cat-1", [new Answer(null!, "Đáp án A", true)], "", ownerUserId, "Toán học");

    [Fact]
    public async Task Handle_QuestionNotFound_ThrowsNotFoundException()
    {
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync("q-1", It.IsAny<CancellationToken>())).ReturnsAsync((Question)null!);
        var handler = new DeleteQuestionCommandHandler(questionRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteQuestionCommand("q-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenExceptionAndDoesNotDelete()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var handler = new DeleteQuestionCommandHandler(questionRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteQuestionCommand(question.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));

        questionRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Owner_DeletesQuestion()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var handler = new DeleteQuestionCommandHandler(questionRepository.Object);

        await handler.Handle(new DeleteQuestionCommand(question.Id, new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        questionRepository.Verify(r => r.DeleteAsync(question.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Admin_DeletesQuestionEvenWhenNotOwner()
    {
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdAsync(question.Id, It.IsAny<CancellationToken>())).ReturnsAsync(question);
        var handler = new DeleteQuestionCommandHandler(questionRepository.Object);

        await handler.Handle(new DeleteQuestionCommand(question.Id, new Actor("admin-1", UserRole.Admin)), CancellationToken.None);

        questionRepository.Verify(r => r.DeleteAsync(question.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteQuestionCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("DeleteQuestionCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, bool isValid)
    {
        var validator = new DeleteQuestionCommandValidator();

        var result = validator.Validate(new DeleteQuestionCommand(id, new Actor("teacher-1", UserRole.Instructor)));

        Assert.Equal(isValid, result.IsValid);
    }
}
