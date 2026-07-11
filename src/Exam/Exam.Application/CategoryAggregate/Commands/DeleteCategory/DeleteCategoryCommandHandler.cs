using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.Services;
using MediatR;

namespace Exam.Application.CategoryAggregate.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CategoryDeletionGuard _categoryDeletionGuard;

    public DeleteCategoryCommandHandler(ICategoryRepository categoryRepository, CategoryDeletionGuard categoryDeletionGuard)
    {
        _categoryRepository = categoryRepository;
        _categoryDeletionGuard = categoryDeletionGuard;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.Id);

        await _categoryDeletionGuard.EnsureCanDeleteAsync(category.Id, cancellationToken);

        await _categoryRepository.DeleteAsync(category.Id, cancellationToken);
    }
}
