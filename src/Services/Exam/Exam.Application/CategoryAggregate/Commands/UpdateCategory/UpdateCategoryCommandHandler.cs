using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public UpdateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        category.Rename(request.Name);
        category.ChangeUrlPath(request.UrlPath);

        await _categoryRepository.UpdateAsync(category, cancellationToken);

        return new CategoryDto(category.Id, category.Name, category.UrlPath);
    }
}
