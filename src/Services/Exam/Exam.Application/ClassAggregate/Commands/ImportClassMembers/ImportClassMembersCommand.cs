using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

// FileContent (byte[]) sẽ bị serialize thành chuỗi base64 khổng lồ nếu AuditLoggingBehavior tự log ->
// bỏ qua auto-log, kết quả import (số dòng thêm/lỗi) đã hiển thị đủ ở UI ngay sau khi gọi.
public record ImportClassMembersCommand(string ClassId, byte[] FileContent, Actor Actor, bool DryRun)
    : IRequest<ImportClassMembersResultDto>, ISkipAutoAuditLog;
