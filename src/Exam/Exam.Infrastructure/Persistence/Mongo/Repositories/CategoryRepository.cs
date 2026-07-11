using Exam.Domain.AggregateModels.CategoryAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class CategoryRepository : MongoRepositoryBase<Category>, ICategoryRepository
{
    public CategoryRepository(MongoDbContext context, ILogger<CategoryRepository> logger, IMediator mediator)
        : base(context, "categories", logger, mediator)
    {
    }
}
