namespace Exam.Application.QuestionAggregate;

internal static class QuestionMapper
{
    public static QuestionDto ToDto(Domain.AggregateModels.QuestionAggregate.Question question) =>
        new(
            question.Id,
            question.Content,
            question.QuestionType,
            question.Level,
            question.CategoryId,
            question.CategoryName,
            question.Answers.Select(a => new AnswerDto(a.Id, a.Content, a.IsCorrect)).ToList(),
            question.Explain,
            question.OwnerUserId);
}
