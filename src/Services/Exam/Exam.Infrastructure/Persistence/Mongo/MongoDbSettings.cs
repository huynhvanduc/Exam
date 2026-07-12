using System.ComponentModel.DataAnnotations;

namespace Exam.Infrastructure.Persistence.Mongo;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    [Required]
    public string DatabaseName { get; set; } = string.Empty;
}
