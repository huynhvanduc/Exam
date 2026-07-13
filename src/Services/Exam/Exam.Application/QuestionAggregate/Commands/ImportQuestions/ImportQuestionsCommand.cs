using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.QuestionAggregate.Commands.ImportQuestions;

// FileContent (byte[]) sẽ bị serialize thành chuỗi base64 khổng lồ nếu AuditLoggingBehavior tự log ->
// bỏ qua auto-log, kết quả import (số dòng thành công/lỗi) đã hiển thị đủ ở UI ngay sau khi gọi.
// DryRun=true: chỉ parse + validate, KHÔNG insert - dùng để hiển thị dialog xem trước trước khi admin xác nhận.
public record ImportQuestionsCommand(byte[] FileContent, string OwnerUserId, bool DryRun) : IRequest<ImportQuestionsResultDto>, ISkipAutoAuditLog;
