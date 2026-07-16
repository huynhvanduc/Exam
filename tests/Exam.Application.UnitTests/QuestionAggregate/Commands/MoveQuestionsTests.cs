using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.QuestionAggregate.Commands.MoveQuestions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.QuestionAggregate.Commands;

public class MoveQuestionsCommandHandlerTests
{
    private static Question CreateQuestion(string ownerUserId) => new(null!, "Nội dung", QuestionType.SingleSelection,
        Level.Easy, "cat-1", [new Answer(null!, "Đáp án A", true)], "", ownerUserId, "Toán học");

    [Fact]
    public async Task Handle_TargetCategoryNotFound_ThrowsNotFoundException()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-2", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new MoveQuestionsCommandHandler(new Mock<IQuestionRepository>().Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MoveQuestionsCommand(["q-1"], "cat-2", new Actor("teacher-1", UserRole.Instructor)),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SomeQuestionMissing_ThrowsNotFoundExceptionAndDoesNotUpdateAny()
    {
        var targetCategory = Category.Create("Vật lý", "vat-ly");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(targetCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(targetCategory);
        var question = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question]);
        var handler = new MoveQuestionsCommandHandler(questionRepository.Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MoveQuestionsCommand([question.Id, "missing-id"], targetCategory.Id,
                new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));

        questionRepository.Verify(r => r.UpdateAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdminForAnyQuestion_ThrowsForbiddenExceptionAndDoesNotUpdateAny()
    {
        var targetCategory = Category.Create("Vật lý", "vat-ly");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(targetCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(targetCategory);
        var ownQuestion = CreateQuestion("teacher-1");
        var otherQuestion = CreateQuestion("other-teacher");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ownQuestion, otherQuestion]);
        var handler = new MoveQuestionsCommandHandler(questionRepository.Object, categoryRepository.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new MoveQuestionsCommand([ownQuestion.Id, otherQuestion.Id], targetCategory.Id,
                new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));

        questionRepository.Verify(r => r.UpdateAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_MovesAllQuestionsToTargetCategory()
    {
        var targetCategory = Category.Create("Vật lý", "vat-ly").WithId("cat-2");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(targetCategory.Id, It.IsAny<CancellationToken>())).ReturnsAsync(targetCategory);
        var question1 = CreateQuestion("teacher-1");
        var question2 = CreateQuestion("teacher-1");
        var questionRepository = new Mock<IQuestionRepository>();
        questionRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([question1, question2]);
        var handler = new MoveQuestionsCommandHandler(questionRepository.Object, categoryRepository.Object);

        await handler.Handle(new MoveQuestionsCommand([question1.Id, question2.Id], targetCategory.Id,
            new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None);

        Assert.Equal(targetCategory.Id, question1.CategoryId);
        Assert.Equal(targetCategory.Name, question1.CategoryName);
        Assert.Equal(targetCategory.Id, question2.CategoryId);
        questionRepository.Verify(r => r.UpdateAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
