using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.ImportQuestions;

// FileContent (byte[]) sẽ bị serialize thành chuỗi base64 khổng lồ nếu AuditLoggingBehavior tự log ->
// bỏ qua auto-log, kết quả import (số dòng thành công/lỗi) đã hiển thị đủ ở UI ngay sau khi gọi.
public record ImportQuestionsCommand(byte[] FileContent, string OwnerUserId) : IRequest<ImportQuestionsResultDto>, ISkipAutoAuditLog;
