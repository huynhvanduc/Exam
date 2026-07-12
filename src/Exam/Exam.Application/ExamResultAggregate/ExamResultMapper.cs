using System.Security.Cryptography;
using System.Text;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;

namespace Exam.Application.ExamResultAggregate;

internal static class ExamResultMapper
{
    public static ExamAttemptDto ToAttemptDto(ExamResult examResult, IReadOnlyCollection<Question> questions)
    {
        var questionsById = questions.ToDictionary(q => q.Id);

        // Xáo thứ tự câu hỏi/đáp án phía server (chống gian lận - client không thể lấy được thứ tự gốc
        // qua Network tab). Seed lấy từ ExamResult.Id nên ổn định trong suốt 1 lượt thi (refresh trang
        // không bị đảo lại), nhưng khác nhau giữa các lượt thi/học viên.
        var orderedQuestions = DeterministicShuffle(examResult.QuestionIds.Where(questionsById.ContainsKey), examResult.Id)
            .Select(id => questionsById[id])
            .Select(q => new ExamAttemptQuestionDto(
                q.Id,
                q.Content,
                q.QuestionType,
                q.Level,
                q.Points,
                DeterministicShuffle(q.Answers, examResult.Id + q.Id)
                    .Select(a => new ExamAttemptAnswerOptionDto(a.Id, a.Content))
                    .ToList()))
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

    private static List<T> DeterministicShuffle<T>(IEnumerable<T> source, string seed)
    {
        var list = source.ToList();
        var rng = new Random(StableSeed(seed));

        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    private static int StableSeed(string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt32(hash, 0);
    }
}
