using ClosedXML.Excel;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.ExportClassMembers;

public class ExportClassMembersQueryHandler : IRequestHandler<ExportClassMembersQuery, byte[]>
{
    private static readonly string[] Headers = ["Họ tên", "Email"];

    private readonly IClassRoomRepository _classRoomRepository;
    private readonly IUserRepository _userRepository;

    public ExportClassMembersQueryHandler(IClassRoomRepository classRoomRepository, IUserRepository userRepository)
    {
        _classRoomRepository = classRoomRepository;
        _userRepository = userRepository;
    }

    public async Task<byte[]> Handle(ExportClassMembersQuery request, CancellationToken cancellationToken)
    {
        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        var members = await _userRepository.GetByExternalIdsAsync(classRoom.MemberUserIds, cancellationToken);
        var memberById = members.ToDictionary(m => m.ExternalId);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Members");

        for (var i = 0; i < Headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var userId in classRoom.MemberUserIds)
        {
            var hasUser = memberById.TryGetValue(userId, out var user);
            worksheet.Cell(rowNumber, 1).Value = hasUser ? $"{user!.FirstName} {user.LastName}".Trim() : "(Không rõ)";
            worksheet.Cell(rowNumber, 2).Value = hasUser ? user!.Email : string.Empty;
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
