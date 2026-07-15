using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

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

        var (exam, questionResults) = await BuildQuestionResultsAsync(examResult, cancellationToken);

        foreach (var questionResult in questionResults)
            examResult.AddQuestionResult(questionResult);

        examResult.Finish(exam?.MinimumPassingScore ?? 0m);
    }

    // Chấm lại 1 bài ĐÃ hoàn thành bằng dữ liệu câu hỏi hiện tại trong ngân hàng (vd sau khi giảng viên
    // sửa lại đáp án đúng bị nhập sai) - dùng khi phát hiện lỗi chấm điểm sau khi học viên đã nộp bài,
    // không có cách nào khác để sửa điểm 1 bài cụ thể trước đây (kết quả bị "đóng băng" vĩnh viễn).
    public async Task RegradeAsync(ExamResult examResult, CancellationToken cancellationToken = default)
    {
        var (exam, questionResults) = await BuildQuestionResultsAsync(examResult, cancellationToken);
        examResult.Regrade(questionResults, exam?.MinimumPassingScore ?? 0m);
    }

    private async Task<(ExamEntity? Exam, List<QuestionResult> QuestionResults)> BuildQuestionResultsAsync(
        ExamResult examResult, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(examResult.ExamId, cancellationToken);
        var questions = await _questionRepository.GetByIdsAsync(examResult.QuestionIds, cancellationToken);
        var questionsById = questions.ToDictionary(q => q.Id);
        var draftByQuestionId = examResult.DraftAnswers.ToDictionary(d => d.QuestionId, d => d.SelectedAnswerIds);

        var questionResults = new List<QuestionResult>();
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

            questionResults.Add(new QuestionResult(question.Id, question.Content, question.QuestionType,
                question.Level, answerResults, question.Explain));
        }

        return (exam, questionResults);
    }
}
