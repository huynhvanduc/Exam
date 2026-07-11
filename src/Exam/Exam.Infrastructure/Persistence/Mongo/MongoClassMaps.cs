using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.SeedWork;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Exam.Infrastructure.Persistence.Mongo;

public static class MongoClassMaps
{
    private static readonly object Lock = new();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        lock (Lock)
        {
            if (_registered)
                return;

            RegisterClassMaps();
            _registered = true;
        }
    }

    private static void RegisterClassMaps()
    {
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new EnumRepresentationConvention(BsonType.String),
        };
        ConventionRegistry.Register(
            "Exam.Domain conventions",
            conventionPack,
            t => t.Namespace != null && t.Namespace.StartsWith("Exam.Domain"));

        if (!BsonClassMap.IsClassMapRegistered(typeof(Entity)))
        {
            BsonClassMap.RegisterClassMap<Entity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(e => e.Id)
                    .SetElementName("_id")
                    .SetSerializer(new StringSerializer(BsonType.ObjectId))
                    .SetIdGenerator(StringObjectIdGenerator.Instance);
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Category)))
        {
            BsonClassMap.RegisterClassMap<Category>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Exam.Domain.AggregateModels.ExamAggregate.Exam)))
        {
            BsonClassMap.RegisterClassMap<Exam.Domain.AggregateModels.ExamAggregate.Exam>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Question)))
        {
            BsonClassMap.RegisterClassMap<Question>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(q => new Question(q.Id, q.Content, q.QuestionType, q.Level, q.CategoryId,
                    q.Answers, q.Explain, q.Points, q.OwnerUserId, q.CategoryName));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(QuestionResult)))
        {
            BsonClassMap.RegisterClassMap<QuestionResult>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(q => new QuestionResult(q.Id, q.Content, q.QuestionType, q.Level,
                    q.Answers, q.Explain, q.Points));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ExamResult)))
        {
            BsonClassMap.RegisterClassMap<ExamResult>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm => cm.AutoMap());
        }
    }
}
