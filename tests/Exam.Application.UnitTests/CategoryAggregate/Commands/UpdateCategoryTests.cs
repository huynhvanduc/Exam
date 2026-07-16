using Exam.Application.CategoryAggregate.Commands.UpdateCategory;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.CategoryAggregate.Commands;

public class UpdateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByIdAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new UpdateCategoryCommandHandler(repository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateCategoryCommand("cat-1", "Toán học", "toan-hoc"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesAndReturnsDto()
    {
        var category = Category.Create("Toán học", "toan-hoc");
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var handler = new UpdateCategoryCommandHandler(repository.Object);

        var result = await handler.Handle(new UpdateCategoryCommand(category.Id, "Vật lý", "vat-ly"), CancellationToken.None);

        Assert.Equal("Vật lý", result.Name);
        Assert.Equal("vat-ly", result.UrlPath);
        repository.Verify(r => r.UpdateAsync(It.Is<Category>(c => c.Name == "Vật lý" && c.UrlPath == "vat-ly"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidName_ThrowsDomainException()
    {
        var category = Category.Create("Toán học", "toan-hoc");
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var handler = new UpdateCategoryCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new UpdateCategoryCommand(category.Id, "", "vat-ly"), CancellationToken.None));
    }
}

public class UpdateCategoryCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("UpdateCategoryCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string id, string name, string urlPath, bool isValid)
    {
        var validator = new UpdateCategoryCommandValidator();

        var result = validator.Validate(new UpdateCategoryCommand(id, name, urlPath));

        Assert.Equal(isValid, result.IsValid);
    }
}
