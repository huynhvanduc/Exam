using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ExamResultRepository : MongoRepositoryBase<ExamResult>, IExamResultRepository
{
    public ExamResultRepository(MongoDbContext context, ILogger<ExamResultRepository> logger, IMediator mediator)
        : base(context, "examResults", logger, mediator)
    {
    }
}
