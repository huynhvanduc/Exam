using ClosedXML.Excel;
using Exam.Application.ClassAggregate.Queries.ExportClassMembers;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Queries;

public class ExportClassMembersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ClassNotFound_ThrowsNotFoundException()
    {
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync((ClassRoom)null!);
        var handler = new ExportClassMembersQueryHandler(classRoomRepository.Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ExportClassMembersQuery("class-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var handler = new ExportClassMembersQueryHandler(classRoomRepository.Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ExportClassMembersQuery(classRoom.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ProducesExcelWithMemberRows()
    {
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1");
        classRoom.AddMember("student-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync(classRoom.Id, It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new User("student-1", "student1@example.com", "Nguyễn", "Văn A")]);
        var handler = new ExportClassMembersQueryHandler(classRoomRepository.Object, userRepository.Object);

        var bytes = await handler.Handle(new ExportClassMembersQuery(classRoom.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Họ tên", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Nguyễn Văn A", worksheet.Cell(2, 1).GetString());
        Assert.Equal("student1@example.com", worksheet.Cell(2, 2).GetString());
    }
}
