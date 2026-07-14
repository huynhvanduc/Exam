namespace Exam.WebApp.Components.Pages.Admin;

// Gộp kết quả form tạo/sửa đề thi: thông tin cơ bản + các cấu hình tuỳ chọn áp dụng tuần tự sau khi
// tạo/sửa. Field nào null nghĩa là không thay đổi/không bật.
public record ExamFormResult(
    ExamRequest Exam,
    ScheduleExamAvailabilityRequest? Availability,
    ConfigureExamCompositionRequest? Composition,
    ConfigureMaxAttemptsRequest? MaxAttempts);
