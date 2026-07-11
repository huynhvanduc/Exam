using Exam.Domain.AggregateModels.CategoryAggregate;
using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.CreateCategory;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = Category.Create(request.Name, request.UrlPath);

        await _categoryRepository.InsertAsync(category, cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.UrlPath);
    }
}
