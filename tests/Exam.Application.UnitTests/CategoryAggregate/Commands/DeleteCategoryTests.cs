using Exam.Application.CategoryAggregate.Commands.DeleteCategory;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.Exceptions;
using Exam.Domain.Services;

namespace Exam.Application.UnitTests.CategoryAggregate.Commands;

public class DeleteCategoryCommandHandlerTests
{
    private static (Mock<ICategoryRepository> CategoryRepository, Mock<IExamRepository> ExamRepository,
        Mock<IQuestionRepository> QuestionRepository, DeleteCategoryCommandHandler Handler) CreateSut()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        var examRepository = new Mock<IExamRepository>();
        var questionRepository = new Mock<IQuestionRepository>();
        var guard = new CategoryDeletionGuard(examRepository.Object, questionRepository.Object);
        var handler = new DeleteCategoryCommandHandler(categoryRepository.Object, guard);

        return (categoryRepository, examRepository, questionRepository, handler);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var (categoryRepository, _, _, handler) = CreateSut();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteCategoryCommand("cat-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CategoryHasExams_ThrowsDomainException()
    {
        var (categoryRepository, examRepository, _, handler) = CreateSut();
        var category = Category.Create("Toán học", "toan-hoc");
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        examRepository.Setup(r => r.ExistsByCategoryIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None));

        categoryRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CategoryHasQuestions_ThrowsDomainException()
    {
        var (categoryRepository, examRepository, questionRepository, handler) = CreateSut();
        var category = Category.Create("Toán học", "toan-hoc");
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        examRepository.Setup(r => r.ExistsByCategoryIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        questionRepository.Setup(r => r.ExistsByCategoryIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None));

        categoryRepository.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_DeletesCategory()
    {
        var (categoryRepository, examRepository, questionRepository, handler) = CreateSut();
        var category = Category.Create("Toán học", "toan-hoc");
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        examRepository.Setup(r => r.ExistsByCategoryIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        questionRepository.Setup(r => r.ExistsByCategoryIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await handler.Handle(new DeleteCategoryCommand(category.Id), CancellationToken.None);

        categoryRepository.Verify(r => r.DeleteAsync(category.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class DeleteCategoryCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("DeleteCategoryCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, bool isValid)
    {
        var validator = new DeleteCategoryCommandValidator();

        var result = validator.Validate(new DeleteCategoryCommand(id));

        Assert.Equal(isValid, result.IsValid);
    }
}
