using Exam.Application.CategoryAggregate.Commands.CreateCategory;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.Exceptions;

namespace Exam.Application.UnitTests.CategoryAggregate.Commands;

public class CreateCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_InsertsCategoryAndReturnsDto()
    {
        var repository = new Mock<ICategoryRepository>();
        var handler = new CreateCategoryCommandHandler(repository.Object);

        var result = await handler.Handle(new CreateCategoryCommand("Toán học", "toan-hoc"), CancellationToken.None);

        Assert.Equal("Toán học", result.Name);
        Assert.Equal("toan-hoc", result.UrlPath);
        repository.Verify(r => r.InsertAsync(It.Is<Category>(c => c.Name == "Toán học" && c.UrlPath == "toan-hoc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidName_ThrowsDomainExceptionAndDoesNotInsert()
    {
        var repository = new Mock<ICategoryRepository>();
        var handler = new CreateCategoryCommandHandler(repository.Object);

        await Assert.ThrowsAsync<ExamDomainException>(() =>
            handler.Handle(new CreateCategoryCommand("", "toan-hoc"), CancellationToken.None));

        repository.Verify(r => r.InsertAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

public class CreateCategoryCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("CreateCategoryCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string name, string urlPath, bool isValid)
    {
        var validator = new CreateCategoryCommandValidator();

        var result = validator.Validate(new CreateCategoryCommand(name, urlPath));

        Assert.Equal(isValid, result.IsValid);
    }

    [Fact]
    public void Validate_NameExceeds200Characters_IsInvalid()
    {
        var validator = new CreateCategoryCommandValidator();
        var longName = new string('a', 201);

        var result = validator.Validate(new CreateCategoryCommand(longName, "toan-hoc"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NameAt200Characters_IsValid()
    {
        var validator = new CreateCategoryCommandValidator();
        var name = new string('a', 200);

        var result = validator.Validate(new CreateCategoryCommand(name, "toan-hoc"));

        Assert.True(result.IsValid);
    }
}
