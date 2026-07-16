using Exam.Application.CategoryAggregate.Queries.GetCategoryById;
using Exam.Domain.AggregateModels.CategoryAggregate;

namespace Exam.Application.UnitTests.CategoryAggregate.Queries;

public class GetCategoryByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingId_ReturnsDto()
    {
        var category = Category.Create("Toán học", "toan-hoc");
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var handler = new GetCategoryByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoryByIdQuery(category.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Toán học", result!.Name);
    }

    [Fact]
    public async Task Handle_NonExistingId_ReturnsNull()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByIdAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new GetCategoryByIdQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoryByIdQuery("missing"), CancellationToken.None);

        Assert.Null(result);
    }
}
