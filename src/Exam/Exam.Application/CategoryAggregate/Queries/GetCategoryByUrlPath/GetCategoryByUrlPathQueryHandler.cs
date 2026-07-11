using Exam.Domain.AggregateModels.CategoryAggregate;
using MediatR;

namespace Exam.Application.CategoryAggregate.Queries.GetCategoryByUrlPath;

public class GetCategoryByUrlPathQueryHandler : IRequestHandler<GetCategoryByUrlPathQuery, CategoryDto?>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryByUrlPathQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto?> Handle(GetCategoryByUrlPathQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByUrlPathAsync(request.UrlPath, cancellationToken);

        return category == null ? null : new CategoryDto(category.Id, category.Name, category.UrlPath);
    }
}
