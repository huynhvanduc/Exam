using Exam.Domain.Enums;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public class QuestionResult : Entity
{
    public string Content { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public Level Level { get; private set; }

    public string Explain { get; private set; }

    public int Points { get; private set; }

    public IReadOnlyCollection<AnswerResult> Answers { get; private set; }

    public bool Result { get; private set; }

    public bool IsAnswered => Answers.Any(a => a.UserChosen == true);

    private QuestionResult()
    {
    }

    public QuestionResult(string id, string content, QuestionType questionType, Level level,
        IReadOnlyCollection<AnswerResult> answers, string explain, int points)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Question content is required.");

        if (answers == null || answers.Count == 0)
            throw new ExamDomainException("Question result must have at least one answer.");

        if (points <= 0)
            throw new ExamDomainException("Question points must be greater than zero.");

        Id = id;
        Content = content;
        QuestionType = questionType;
        Level = level;
        Explain = explain;
        Points = points;
        Answers = answers;
        Result = answers.All(a => a.IsCorrect == (a.UserChosen == true));
    }
}
