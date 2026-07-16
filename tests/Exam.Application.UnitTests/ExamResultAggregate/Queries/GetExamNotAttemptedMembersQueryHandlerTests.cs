using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.GetExamNotAttemptedMembers;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ClassAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using Exam.Domain.AggregateModels.UserAggregate;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class GetExamNotAttemptedMembersQueryHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) =>
        new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
            ownerUserId, "cat-1", "Toán học", true, 5m).WithId("exam-1");

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new GetExamNotAttemptedMembersQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object,
            new Mock<IClassRoomRepository>().Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetExamNotAttemptedMembersQuery("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new GetExamNotAttemptedMembersQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object,
            new Mock<IClassRoomRepository>().Object, new Mock<IUserRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetExamNotAttemptedMembersQuery(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PublicExam_ReturnsEmptyList()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new GetExamNotAttemptedMembersQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object,
            new Mock<IClassRoomRepository>().Object, new Mock<IUserRepository>().Object);

        var result = await handler.Handle(new GetExamNotAttemptedMembersQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsOnlyMembersWhoHaveNotAttempted()
    {
        var exam = CreateExam("teacher-1");
        exam.AssignToClass("class-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1").WithId("class-1");
        classRoom.AddMember("student-1");
        classRoom.AddMember("student-2");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetAttemptedUserIdsByExamIdAsync(exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["student-1"]);
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByExternalIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new User("student-2", "student2@example.com", "Trần", "Thị B")]);
        var handler = new GetExamNotAttemptedMembersQueryHandler(examRepository.Object, examResultRepository.Object,
            classRoomRepository.Object, userRepository.Object);

        var result = await handler.Handle(new GetExamNotAttemptedMembersQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("student-2", result.First().UserId);
    }

    [Fact]
    public async Task Handle_AllMembersAttempted_ReturnsEmptyList()
    {
        var exam = CreateExam("teacher-1");
        exam.AssignToClass("class-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var classRoom = ClassRoom.Create("Lớp 10A1", "teacher-1").WithId("class-1");
        classRoom.AddMember("student-1");
        var classRoomRepository = new Mock<IClassRoomRepository>();
        classRoomRepository.Setup(r => r.GetByIdAsync("class-1", It.IsAny<CancellationToken>())).ReturnsAsync(classRoom);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.GetAttemptedUserIdsByExamIdAsync(exam.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["student-1"]);
        var handler = new GetExamNotAttemptedMembersQueryHandler(examRepository.Object, examResultRepository.Object,
            classRoomRepository.Object, new Mock<IUserRepository>().Object);

        var result = await handler.Handle(new GetExamNotAttemptedMembersQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        Assert.Empty(result);
    }
}
