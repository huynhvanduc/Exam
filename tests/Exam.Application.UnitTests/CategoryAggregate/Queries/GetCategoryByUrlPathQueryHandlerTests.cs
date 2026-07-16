using Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;
using Exam.Domain.AggregateModels.CategoryAggregate;

namespace Exam.Application.UnitTests.CategoryAggregate.Queries;

public class GetCategoryByUrlPathQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUrlPath_ReturnsDto()
    {
        var category = Category.Create("Toán học", "toan-hoc");
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByUrlPathAsync("toan-hoc", It.IsAny<CancellationToken>())).ReturnsAsync(category);
        var handler = new GetCategoryByUrlPathQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoryByUrlPathQuery("toan-hoc"), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Toán học", result!.Name);
    }

    [Fact]
    public async Task Handle_NonExistingUrlPath_ReturnsNull()
    {
        var repository = new Mock<ICategoryRepository>();
        repository.Setup(r => r.GetByUrlPathAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((Category)null!);
        var handler = new GetCategoryByUrlPathQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCategoryByUrlPathQuery("missing"), CancellationToken.None);

        Assert.Null(result);
    }
}
