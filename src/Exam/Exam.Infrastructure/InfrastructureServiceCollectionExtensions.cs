using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.Events;
using Exam.Domain.Services;
using Exam.Infrastructure.Persistence.Mongo;
using Exam.Infrastructure.Persistence.Mongo.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Exam.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MongoDbSettings>()
            .Bind(configuration.GetSection(MongoDbSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(sp =>
            MongoClientFactory.Create(sp.GetRequiredService<IOptions<MongoDbSettings>>().Value));

        services.AddSingleton<MongoDbContext>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ExamStartDomainEvent).Assembly));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IExamRepository, ExamRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IExamResultRepository, ExamResultRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<ExamQuestionPoolService>();

        services.AddHealthChecks()
            .AddCheck<MongoDbHealthCheck>("mongodb");

        return services;
    }
}
