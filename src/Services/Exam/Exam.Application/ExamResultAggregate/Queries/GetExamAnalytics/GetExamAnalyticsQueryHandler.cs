using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Contracts;
using MediatR;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAnalytics;

public class GetExamAnalyticsQueryHandler : IRequestHandler<GetExamAnalyticsQuery, ExamAnalyticsDto>
{
    // Ngưỡng tối thiểu số lượt trả lời để 1 câu hỏi được tính vào "hay bị sai nhất" - tránh 1-2 lượt lẻ
    // tạo tỷ lệ sai 100% gây nhiễu.
    private const int MinAnswersForHardestQuestion = 3;
    private static readonly (string Label, decimal From, decimal To)[] HistogramBuckets =
    [
        ("0-2", 0m, 2m), ("2-4", 2m, 4m), ("4-6", 4m, 6m), ("6-8", 6m, 8m), ("8-10", 8m, 10m)
    ];

    private readonly IExamRepository _examRepository;
    private readonly IExamResultRepository _examResultRepository;

    public GetExamAnalyticsQueryHandler(IExamRepository examRepository, IExamResultRepository examResultRepository)
    {
        _examRepository = examRepository;
        _examResultRepository = examResultRepository;
    }

    public async Task<ExamAnalyticsDto> Handle(GetExamAnalyticsQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamEntity), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(ExamEntity), exam.Id);

        var total = await _examResultRepository.CountByExamIdAsync(request.ExamId, cancellationToken);
        var results = await _examResultRepository.GetByExamIdAsync(request.ExamId, 0, (int)total, cancellationToken);
        var finished = results.Where(r => r.Finished).ToList();

        var histogram = HistogramBuckets
            .Select(b => new ScoreHistogramBucketDto(b.Label, finished.Count(r => IsInBucket(r.TotalScore, b.From, b.To))))
            .ToList();

        if (finished.Count == 0)
            return new ExamAnalyticsDto(0, 0m, 0m, histogram, []);

        var averageScore = Math.Round(finished.Average(r => r.TotalScore), 2);
        var passRate = Math.Round(100m * finished.Count(r => r.Passed == true) / finished.Count, 1);

        var hardestQuestions = finished
            .SelectMany(r => r.QuestionResults)
            .GroupBy(qr => qr.Id)
            .Select(g => new
            {
                QuestionId = g.Key,
                Content = g.First().Content,
                Total = g.Count(),
                Incorrect = g.Count(qr => !qr.Result)
            })
            .Where(x => x.Total >= MinAnswersForHardestQuestion)
            .Select(x => new HardestQuestionDto(x.QuestionId, x.Content, x.Total, x.Incorrect, Math.Round(100m * x.Incorrect / x.Total, 1)))
            .OrderByDescending(x => x.IncorrectRate)
            .ThenByDescending(x => x.TotalAnswered)
            .Take(10)
            .ToList();

        return new ExamAnalyticsDto(finished.Count, averageScore, passRate, histogram, hardestQuestions);
    }

    private static bool IsInBucket(decimal score, decimal from, decimal to) =>
        to == 10m ? score >= from && score <= to : score >= from && score < to;
}
