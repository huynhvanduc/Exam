using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.ExamResultAggregate;

internal static class ExamResultMapper
{
    public static ExamAttemptDto ToAttemptDto(ExamResult examResult, IReadOnlyCollection<Question> questions)
    {
        var questionsById = questions.ToDictionary(q => q.Id);

        var orderedQuestions = examResult.QuestionIds
            .Where(questionsById.ContainsKey)
            .Select(id => questionsById[id])
            .Select(q => new ExamAttemptQuestionDto(
                q.Id,
                q.Content,
                q.QuestionType,
                q.Level,
                q.Points,
                q.Answers.Select(a => new ExamAttemptAnswerOptionDto(a.Id, a.Content)).ToList()))
            .ToList();

        var selections = examResult.DraftAnswers
            .Select(d => new ExamAttemptAnswerSelectionDto(d.QuestionId, d.SelectedAnswerIds))
            .ToList();

        return new ExamAttemptDto(examResult.Id, examResult.ExamId, examResult.ExamTitle, examResult.ExamStartDate,
            examResult.Deadline, orderedQuestions, selections);
    }

    public static ExamResultDto ToResultDto(ExamResult examResult) =>
        new(
            examResult.Id,
            examResult.ExamId,
            examResult.ExamTitle,
            examResult.UserId,
            examResult.Email,
            examResult.FullName,
            examResult.TotalScore,
            examResult.MaxPossibleScore,
            examResult.CorrectQuestionCount,
            examResult.Passed,
            examResult.ExamStartDate,
            examResult.ExamFinishDate,
            examResult.Finished,
            examResult.QuestionResults.Select(qr => new QuestionResultDto(
                qr.Id,
                qr.Content,
                qr.QuestionType,
                qr.Level,
                qr.Explain,
                qr.Points,
                qr.Result,
                qr.IsAnswered,
                qr.Answers.Select(a => new AnswerResultDto(a.Id, a.Content, a.UserChosen, a.IsCorrect)).ToList())).ToList());

    public static ExamResultSummaryDto ToSummaryDto(ExamResult examResult) =>
        new(
            examResult.Id,
            examResult.ExamId,
            examResult.ExamTitle,
            examResult.TotalScore,
            examResult.MaxPossibleScore,
            examResult.Passed,
            examResult.ExamStartDate,
            examResult.ExamFinishDate,
            examResult.Finished);
}
