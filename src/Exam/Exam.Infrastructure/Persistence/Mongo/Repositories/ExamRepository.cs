using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;
using Microsoft.Extensions.Logging;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Infrastructure.Persistence.Mongo.Repositories;

public class ExamRepository : MongoRepositoryBase<ExamEntity>, IExamRepository
{
    public ExamRepository(MongoDbContext context, ILogger<ExamRepository> logger, IMediator mediator)
        : base(context, "exams", logger, mediator)
    {
    }
}
