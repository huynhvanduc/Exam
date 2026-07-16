using ClosedXML.Excel;
using Exam.Application.ClassAggregate.Commands.ImportClassMembers;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.AuditAggregate;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class ImportClassMembersCommandHandlerTests
{
    // Dựng file Excel tối giản trong bộ nhớ (1 cột email, có header) để test handler đọc bằng ClosedXML -
    // tránh phải kèm file mẫu nhị phân trong repo.
    private static byte[] BuildExcel(params string?[] emails)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");
        worksheet.Cell(1, 1).Value = "Email";
        for (var i = 0; i < emails.Length; i++)
            worksheet.Cell(i + 2, 1).Value = emails[i] ?? "";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static (Mock<IClassRoomRepository> ClassRoomRepository, Mock<IUserRepository> UserRepository,
        Mock<IAuditLogRepository> AuditLogRepository, ImportClassMembersCommandHandler Handler, ClassRoom ClassRoom) CreateSut()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var userRepository = new Mock<IUserRepository>();
        var auditLogRepository = new Mock<IAuditLogRepository>();
        var handler = new ImportClassMembersCommandHandler(classRoomRepository.Object, userRepository.Object, auditLogRepository.Object);

        return (classRoomRepository, userRepository, auditLogRepository, handler, classRoom);
    }

    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new ImportClassMembersCommandHandler(classRoomRepository.Object, new Mock<IUserRepository>().Object,
            new Mock<IAuditLogRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ImportClassMembersCommand("class-1", BuildExcel("a@example.com"), new Actor("teacher-1", UserRole.Instructor), false),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var (classRoomRepository, _, _, handler, classRoom) = CreateSut();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("a@example.com"),
                new Actor("other-teacher", UserRole.Instructor), false), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EmailNotFound_RecordsRowError()
    {
        var (_, userRepository, _, handler, classRoom) = CreateSut();
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("missing@example.com"),
            new Actor("teacher-1", UserRole.Instructor), false), CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
        Assert.Equal(0, result.AddedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Không tìm thấy tài khoản", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_UserNotStudent_RecordsRowError()
    {
        var (_, userRepository, _, handler, classRoom) = CreateSut();
        var instructor = new User("teacher-2", "teacher2@example.com", "Trần", "Thị B", UserRole.Instructor);
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([instructor]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("teacher2@example.com"),
            new Actor("teacher-1", UserRole.Instructor), false), CancellationToken.None);

        Assert.Single(result.Errors);
        Assert.Contains("không phải học viên", result.Errors.First().Message);
    }

    [Fact]
    public async Task Handle_DryRun_DoesNotModifyClassRoomOrWriteAuditLog()
    {
        var (classRoomRepository, userRepository, auditLogRepository, handler, classRoom) = CreateSut();
        var student = new User("student-1", "student1@example.com", "Nguyễn", "Văn A", UserRole.Student);
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([student]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("student1@example.com"),
            new Actor("teacher-1", UserRole.Instructor), true), CancellationToken.None);

        Assert.Equal(1, result.AddedCount);
        Assert.False(classRoom.HasMember("student-1"));
        classRoomRepository.Verify(r => r.UpdateAsync(It.IsAny<ClassRoom>(), It.IsAny<CancellationToken>()), Times.Never);
        auditLogRepository.Verify(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NotDryRun_AddsMembersAndWritesAuditLog()
    {
        var (classRoomRepository, userRepository, auditLogRepository, handler, classRoom) = CreateSut();
        var student = new User("student-1", "student1@example.com", "Nguyễn", "Văn A", UserRole.Student);
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([student]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("student1@example.com"),
            new Actor("teacher-1", UserRole.Instructor), false), CancellationToken.None);

        Assert.Equal(1, result.AddedCount);
        Assert.True(classRoom.HasMember("student-1"));
        classRoomRepository.Verify(r => r.UpdateAsync(classRoom, It.IsAny<CancellationToken>()), Times.Once);
        auditLogRepository.Verify(r => r.InsertAsync(It.IsAny<AuditLogEntry>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyMember_CountedButNotReAddedOrDuplicated()
    {
        var (classRoomRepository, userRepository, _, handler, classRoom) = CreateSut();
        classRoom.AddMember("student-1");
        var student = new User("student-1", "student1@example.com", "Nguyễn", "Văn A", UserRole.Student);
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([student]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id, BuildExcel("student1@example.com"),
            new Actor("teacher-1", UserRole.Instructor), false), CancellationToken.None);

        Assert.Equal(0, result.AddedCount);
        Assert.Equal(1, result.AlreadyMemberCount);
        Assert.Single(classRoom.MemberUserIds);
    }

    [Fact]
    public async Task Handle_BlankRowsAreSkipped()
    {
        var (_, userRepository, _, handler, classRoom) = CreateSut();
        var student = new User("student-1", "student1@example.com", "Nguyễn", "Văn A", UserRole.Student);
        userRepository.Setup(r => r.GetByEmailsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>())).ReturnsAsync([student]);

        var result = await handler.Handle(new ImportClassMembersCommand(classRoom.Id,
            BuildExcel("student1@example.com", "", null, "   "), new Actor("teacher-1", UserRole.Instructor), true), CancellationToken.None);

        Assert.Equal(1, result.TotalRows);
    }
}

public class ImportClassMembersCommandValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_IsValid()
    {
        var validator = new ImportClassMembersCommandValidator();

        var result = validator.Validate(new ImportClassMembersCommand("class-1", [1, 2, 3],
            new Actor("teacher-1", UserRole.Instructor), false));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyClassId_IsInvalid()
    {
        var validator = new ImportClassMembersCommandValidator();

        var result = validator.Validate(new ImportClassMembersCommand("", [1, 2, 3],
            new Actor("teacher-1", UserRole.Instructor), false));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyFileContent_IsInvalid()
    {
        var validator = new ImportClassMembersCommandValidator();

        var result = validator.Validate(new ImportClassMembersCommand("class-1", [],
            new Actor("teacher-1", UserRole.Instructor), false));

        Assert.False(result.IsValid);
    }
}
