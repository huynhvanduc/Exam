using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Domain.Services;

public class ExamResultGradingService
{
    private readonly IExamRepository _examRepository;
    private readonly IQuestionRepository _questionRepository;

    public ExamResultGradingService(IExamRepository examRepository, IQuestionRepository questionRepository)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
    }

    public async Task GradeAndFinishAsync(ExamResult examResult, CancellationToken cancellationToken = default)
    {
        if (examResult.Finished)
            return;

        var exam = await _examRepository.GetByIdAsync(examResult.ExamId, cancellationToken);
        var questions = await _questionRepository.GetByIdsAsync(examResult.QuestionIds, cancellationToken);
        var questionsById = questions.ToDictionary(q => q.Id);
        var draftByQuestionId = examResult.DraftAnswers.ToDictionary(d => d.QuestionId, d => d.SelectedAnswerIds);

        foreach (var questionId in examResult.QuestionIds)
        {
            if (!questionsById.TryGetValue(questionId, out var question))
                continue;

            var selectedAnswerIds = draftByQuestionId.TryGetValue(questionId, out var ids)
                ? ids
                : Array.Empty<string>();

            var answerResults = question.Answers
                .Select(a => new AnswerResult(a.Id, a.Content, selectedAnswerIds.Contains(a.Id), a.IsCorrect))
                .ToList();

            examResult.AddQuestionResult(new QuestionResult(question.Id, question.Content, question.QuestionType,
                question.Level, answerResults, question.Explain));
        }

        examResult.Finish(exam?.MinimumPassingScore ?? 0m);
    }
}
