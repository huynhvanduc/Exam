using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.QuestionAggregate;

public class Answer : Entity
{
    public string Content { get; private set; }

    public bool IsCorrect { get; private set; }

    private Answer()
    {
    }

    public Answer(string id, string content, bool isCorrect = false)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Answer content is required.");

        Id = id;
        Content = content;
        IsCorrect = isCorrect;
    }
}
