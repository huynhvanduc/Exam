using Exam.Contracts;
using Exam.Domain.Exceptions;
using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.QuestionAggregate;

public class Question : Entity, IAggregateRoot
{
    public string Content { get; private set; }

    public QuestionType QuestionType { get; private set; }

    public Level Level { get; private set; }

    public string CategoryId { get; private set; }

    public string CategoryName { get; private set; }

    public IReadOnlyCollection<Answer> Answers { get; private set; }

    public string Explain { get; private set; }

    public int Points { get; private set; }

    public DateTime DateCreated { get; private set; }

    public string OwnerUserId { get; private set; }

    private Question()
    {
    }

    public Question(string id, string content, QuestionType questionType, Level level, string categoryId,
        IReadOnlyCollection<Answer> answers, string explain, int points = 1, string ownerUserId = null, string categoryName = null)
    {
        EnsureValid(content, categoryId, answers, questionType, points);

        Id = id;
        Content = content;
        QuestionType = questionType;
        Level = level;
        CategoryId = categoryId;
        Answers = answers;
        Explain = explain;
        Points = points;
        DateCreated = DateTime.UtcNow;
        OwnerUserId = ownerUserId;
        CategoryName = categoryName;
    }

    public void Update(string content, QuestionType questionType, Level level, string categoryId, string categoryName,
        IReadOnlyCollection<Answer> answers, string explain, int points)
    {
        EnsureValid(content, categoryId, answers, questionType, points);

        Content = content;
        QuestionType = questionType;
        Level = level;
        CategoryId = categoryId;
        CategoryName = categoryName;
        Answers = answers;
        Explain = explain;
        Points = points;
    }

    public void ChangePoints(int points)
    {
        if (points <= 0)
            throw new ExamDomainException("Question points must be greater than zero.");

        Points = points;
    }

    public void ChangeCategory(string categoryId, string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Question category is required.");

        CategoryId = categoryId;
        CategoryName = categoryName;
    }

    private static void EnsureValid(string content, string categoryId, IReadOnlyCollection<Answer> answers,
        QuestionType questionType, int points)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ExamDomainException("Question content is required.");

        if (string.IsNullOrWhiteSpace(categoryId))
            throw new ExamDomainException("Question category is required.");

        if (answers == null || answers.Count == 0)
            throw new ExamDomainException($"{nameof(answers)} can not be empty.");

        if (questionType == QuestionType.SingleSelection && answers.Count(x => x.IsCorrect) > 1)
            throw new ExamDomainException($"{nameof(answers)} is invalid: a single selection question can only have one correct answer.");

        if (points <= 0)
            throw new ExamDomainException("Question points must be greater than zero.");
    }
}
