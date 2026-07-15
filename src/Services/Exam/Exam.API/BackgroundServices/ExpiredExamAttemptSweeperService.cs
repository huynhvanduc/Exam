using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.Services;

namespace Exam.API.BackgroundServices;

// Fix gap "bài thi bỏ dở treo mãi mãi": trước đây một attempt quá hạn giờ làm chỉ được tự nộp khi CHÍNH
// học viên gọi lại API (RecordAnswer/GetStatus/Finish) sau khi hết giờ, hoặc khi Admin tay bấm "Buộc nộp
// bài" cho từng người. Nếu học viên bắt đầu thi rồi bỏ ngang không quay lại nữa, ExamResult giữ nguyên
// Finished=false vô thời hạn. Service này định kỳ quét toàn bộ attempt đang làm dở và tự chấm+nộp hộ
// những attempt đã quá hạn giờ làm (IsExpired), dùng đúng ExamResultGradingService như luồng nộp bài thật.
public class ExpiredExamAttemptSweeperService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiredExamAttemptSweeperService> _logger;

    public ExpiredExamAttemptSweeperService(IServiceScopeFactory scopeFactory, ILogger<ExpiredExamAttemptSweeperService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sweep expired exam attempts.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var examResultRepository = scope.ServiceProvider.GetRequiredService<IExamResultRepository>();
        var gradingService = scope.ServiceProvider.GetRequiredService<ExamResultGradingService>();

        var inProgress = await examResultRepository.GetAllInProgressAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var expired = inProgress.Where(r => r.IsExpired(now)).ToList();

        foreach (var examResult in expired)
        {
            await gradingService.GradeAndFinishAsync(examResult, cancellationToken);
            await examResultRepository.UpdateAsync(examResult, cancellationToken);
        }

        if (expired.Count > 0)
            _logger.LogInformation("Auto-finished {Count} expired exam attempts.", expired.Count);
    }
}
