using Exam.Application.CategoryAggregate.Queries.GetCategories;
using Exam.Domain.AggregateModels.CategoryAggregate;

namespace Exam.Application.UnitTests.CategoryAggregate.Queries;

public class GetCategoriesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllCategoriesAsDto()
    {
        var categories = new[] { Category.Create("Toán học", "toan-hoc"), Category.Create("Vật lý", "vat-ly") };
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        var handler = new GetCategoriesQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Name == "Toán học");
    }

    [Fact]
    public async Task Handle_NoCategories_ReturnsEmptyList()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var handler = new GetCategoriesQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
