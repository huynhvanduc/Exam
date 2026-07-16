using Exam.Application.Exceptions;
using Exam.Application.ExamAggregate.Commands.CreateExam;
using Exam.Contracts;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;

namespace Exam.Application.UnitTests.ExamAggregate.Commands;

public class CreateExamCommandHandlerTests
{
    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync("cat-1", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new CreateExamCommandHandler(new Mock<IExamRepository>().Object, categoryRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateExamCommand("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(60), Level.Easy,
                "cat-1", true, 5m, "teacher-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_InsertsExamWithCategoryNameAndReturnsDto()
    {
        var category = Category.Create("Toán học", "toan-hoc").WithId("cat-1");
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var examRepository = new Mock<IExamRepository>();
        var handler = new CreateExamCommandHandler(examRepository.Object, categoryRepository.Object);

        var result = await handler.Handle(new CreateExamCommand("Đề 1", "Mô tả", "Nội dung", TimeSpan.FromMinutes(60),
            Level.Easy, category.Id, true, 5m, "teacher-1"), CancellationToken.None);

        Assert.Equal("Đề 1", result.Name);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal(category.Name, result.CategoryName);
        Assert.Equal("teacher-1", result.OwnerUserId);
        Assert.Equal(ExamStatus.Draft, result.Status);
        examRepository.Verify(r => r.InsertAsync(
            It.Is<Domain.AggregateModels.ExamAggregate.Exam>(e => e.CategoryName == category.Name && e.OwnerUserId == "teacher-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CreateExamCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("CreateExamCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string name, string shortDesc, string categoryId, string ownerUserId, bool isValid)
    {
        var validator = new CreateExamCommandValidator();

        var result = validator.Validate(new CreateExamCommand(name, shortDesc, "Nội dung", TimeSpan.FromMinutes(60),
            Level.Easy, categoryId, true, 5m, ownerUserId));

        Assert.Equal(isValid, result.IsValid);
    }

    [Fact]
    public void Validate_NameExceeds200Characters_IsInvalid()
    {
        var validator = new CreateExamCommandValidator();
        var longName = new string('a', 201);

        var result = validator.Validate(new CreateExamCommand(longName, "", "Nội dung", TimeSpan.FromMinutes(60),
            Level.Easy, "cat-1", true, 5m, "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ZeroDuration_IsInvalid()
    {
        var validator = new CreateExamCommandValidator();

        var result = validator.Validate(new CreateExamCommand("Đề 1", "", "Nội dung", TimeSpan.Zero,
            Level.Easy, "cat-1", true, 5m, "teacher-1"));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public void Validate_MinimumPassingScoreBoundary(decimal score, bool isValid)
    {
        var validator = new CreateExamCommandValidator();

        var result = validator.Validate(new CreateExamCommand("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(60),
            Level.Easy, "cat-1", true, score, "teacher-1"));

        Assert.Equal(isValid, result.IsValid);
    }
}
