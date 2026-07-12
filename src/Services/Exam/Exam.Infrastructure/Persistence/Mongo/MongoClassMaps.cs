using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using Exam.Domain.AggregateModels.RoleAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using Exam.Domain.SeedWork;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

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

        if (!BsonClassMap.IsClassMapRegistered(typeof(Answer)))
        {
            BsonClassMap.RegisterClassMap<Answer>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(a => new Answer(a.Id, a.Content, a.IsCorrect));
            });
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

        if (!BsonClassMap.IsClassMapRegistered(typeof(AnswerResult)))
        {
            BsonClassMap.RegisterClassMap<AnswerResult>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(a => new AnswerResult(a.Id, a.Content, a.UserChosen, a.IsCorrect));
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

        if (!BsonClassMap.IsClassMapRegistered(typeof(ExamEntity)))
        {
            BsonClassMap.RegisterClassMap<ExamEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(e => new ExamEntity(e.Name, e.ShortDesc, e.Content, e.Duration, e.Level,
                    e.OwnerUserId, e.CategoryId, e.CategoryName, e.IsTimeRestricted, e.MinimumPassingScore));
                cm.MapMember(e => e.NegativeMarkingRatio).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(DraftAnswer)))
        {
            BsonClassMap.RegisterClassMap<DraftAnswer>(cm => cm.AutoMap());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(ExamResult)))
        {
            BsonClassMap.RegisterClassMap<ExamResult>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(r => new ExamResult(r.UserId, r.ExamId, r.NegativeMarkingRatio));
                cm.MapMember(r => r.NegativeMarkingRatio).SetSerializer(new DecimalSerializer(BsonType.Decimal128));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
        {
            BsonClassMap.RegisterClassMap<User>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(u => new User(u.ExternalId, u.FirstName, u.LastName, u.Role));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(RolePermissionSet)))
        {
            BsonClassMap.RegisterClassMap<RolePermissionSet>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(rp => new RolePermissionSet(rp.Role, rp.Permissions));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(AuditLogEntry)))
        {
            BsonClassMap.RegisterClassMap<AuditLogEntry>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(a => new AuditLogEntry(a.ActorUserId, a.Action, a.TargetId, a.Description));
            });
        }
    }
}
