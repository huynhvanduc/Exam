using ClosedXML.Excel;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

public class ImportClassMembersCommandHandler : IRequestHandler<ImportClassMembersCommand, ImportClassMembersResultDto>
{
    private const int HeaderRowNumber = 1;

    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public ImportClassMembersCommandHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository,
        IAuditLogRepository auditLogRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ImportClassMembersResultDto> Handle(ImportClassMembersCommand request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        using var workbook = new XLWorkbook(new MemoryStream(request.FileContent));
        var worksheet = workbook.Worksheets.First();
        var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? HeaderRowNumber;

        var rowEmails = new List<(int RowNumber, string Email)>();
        var totalRows = 0;

        for (var rowNumber = HeaderRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var email = worksheet.Row(rowNumber).Cell(1).GetString().Trim();

            // Bỏ qua dòng trống hoàn toàn (thường gặp ở cuối file).
            if (string.IsNullOrWhiteSpace(email))
                continue;

            totalRows++;
            rowEmails.Add((rowNumber, email));
        }

        var users = await _userRepository.GetByEmailsAsync(rowEmails.Select(r => r.Email), cancellationToken);
        var userByEmail = users.ToDictionary(u => u.Email.Trim(), u => u, StringComparer.OrdinalIgnoreCase);

        var errors = new List<ImportClassMemberRowError>();
        var validRows = new List<ImportClassMemberPreviewRow>();
        var usersToAdd = new List<User>();

        foreach (var (rowNumber, email) in rowEmails)
        {
            if (!userByEmail.TryGetValue(email, out var user))
            {
                errors.Add(new ImportClassMemberRowError(rowNumber, email, "Không tìm thấy tài khoản với email này."));
                continue;
            }

            if (user.Role != UserRole.Student)
            {
                errors.Add(new ImportClassMemberRowError(rowNumber, email, "Tài khoản này không phải học viên."));
                continue;
            }

            var alreadyMember = classRoom.MemberUserIds.Contains(user.ExternalId);
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            validRows.Add(new ImportClassMemberPreviewRow(rowNumber, email, fullName, alreadyMember));

            if (!alreadyMember)
                usersToAdd.Add(user);
        }

        if (!request.DryRun && usersToAdd.Count > 0)
        {
            foreach (var user in usersToAdd)
                classRoom.AddMember(user.ExternalId);

            await _classRoomRepository.UpdateAsync(classRoom, cancellationToken);

            // ImportClassMembersCommand tự khai ISkipAutoAuditLog (FileContent quá nặng để log tự động) nên
            // phải tự ghi log thủ công ở đây - trước đây import roster hàng loạt hoàn toàn không có dấu vết.
            await _auditLogRepository.InsertAsync(
                new AuditLogEntry(request.Actor.UserId, "Class.ImportMembers", classRoom.Id,
                    $"Import {usersToAdd.Count} học viên từ Excel vào lớp \"{classRoom.Name}\"."),
                cancellationToken);
        }

        var alreadyMemberCount = validRows.Count(r => r.AlreadyMember);

        return new ImportClassMembersResultDto(totalRows, usersToAdd.Count, alreadyMemberCount, errors, validRows);
    }
}
