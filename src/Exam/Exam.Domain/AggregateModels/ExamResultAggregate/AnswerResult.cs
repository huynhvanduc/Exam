using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class AnswerResult : Entity
{
    public string Content { get; private set; }

    public bool? UserChosen { get; private set; }

    public bool IsCorrect { get; private set; }

    private AnswerResult()
    {
    }

    public AnswerResult(string id, string content, bool? userChosen, bool isCorrect)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Answer content is required.");

        Id = id;
        Content = content;
        UserChosen = userChosen;
        IsCorrect = isCorrect;
    }
}
