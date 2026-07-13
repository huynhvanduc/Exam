namespace Exam.WebApp.Components.Pages.Admin;

// Gộp kết quả form tạo/sửa đề thi: thông tin cơ bản + 4 cấu hình tuỳ chọn (trước đây phải vào
// ExamDetail cấu hình riêng sau khi tạo). Field nào null nghĩa là không thay đổi/không bật.
public record ExamFormResult(
    ExamRequest Exam,
    ScheduleExamAvailabilityRequest? Availability,
    ConfigureNegativeMarkingRequest? NegativeMarking,
    ConfigureQuestionPoolRequest? Pool,
    ConfigureMaxAttemptsRequest? MaxAttempts);
