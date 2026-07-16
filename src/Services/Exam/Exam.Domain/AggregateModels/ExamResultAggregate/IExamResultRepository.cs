using Exam.Domain.SeedWork;

namespace Exam.Domain.AggregateModels.ExamResultAggregate;

public interface IExamResultRepository : IRepositoryBase<ExamResult>
{
    Task<ExamResult> GetInProgressAttemptAsync(string userId, string examId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExamResult>> GetByUserIdAsync(string userId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExamResult>> GetByExamIdAsync(string examId, int skip, int take, CancellationToken cancellationToken = default);

    Task<long> CountByExamIdAsync(string examId, CancellationToken cancellationToken = default);

    // Đếm MỌI attempt (kể cả bỏ dở chưa nộp) - mỗi lần StartExam tạo mới đã tính 1 lượt.
    Task<long> CountByUserIdAndExamIdAsync(string userId, string examId, CancellationToken cancellationToken = default);

    // Toàn bộ attempt đang làm dở (Finished = false), bất kể user/exam nào - dùng cho tiến trình nền quét
    // và tự nộp hộ các bài đã quá hạn nhưng học viên bỏ ngang không quay lại (xem ExpiredExamAttemptSweeperService).
    Task<IReadOnlyCollection<ExamResult>> GetAllInProgressAsync(CancellationToken cancellationToken = default);

    // UserId duy nhất của mọi người đã có ít nhất 1 attempt (kể cả đang làm dở) cho đề thi này - dùng để suy
    // ra "còn ai trong lớp CHƯA làm bài" (lấy danh sách thành viên lớp trừ đi tập này).
    Task<IReadOnlyCollection<string>> GetAttemptedUserIdsByExamIdAsync(string examId, CancellationToken cancellationToken = default);
}
