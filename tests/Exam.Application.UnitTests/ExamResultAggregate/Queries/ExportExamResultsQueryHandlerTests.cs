using ClosedXML.Excel;
using Exam.Application.Common;
using Exam.Application.Exceptions;
using Exam.Application.ExamResultAggregate.Queries.ExportExamResults;
using Exam.Contracts;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;

namespace Exam.Application.UnitTests.ExamResultAggregate.Queries;

public class ExportExamResultsQueryHandlerTests
{
    private static Domain.AggregateModels.ExamAggregate.Exam CreateExam(string ownerUserId) =>
        new Domain.AggregateModels.ExamAggregate.Exam("Đề 1", "", "Nội dung", TimeSpan.FromMinutes(30), Level.Easy,
            ownerUserId, "cat-1", "Toán học", true, 5m).WithId("exam-1");

    [Fact]
    public async Task Handle_ExamNotFound_ThrowsNotFoundException()
    {
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync("exam-1", It.IsAny<CancellationToken>())).ReturnsAsync((Domain.AggregateModels.ExamAggregate.Exam)null!);
        var handler = new ExportExamResultsQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ExportExamResultsQuery("exam-1", new Actor("teacher-1", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NotOwnerOrAdmin_ThrowsForbiddenException()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var handler = new ExportExamResultsQueryHandler(examRepository.Object, new Mock<IExamResultRepository>().Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ExportExamResultsQuery(exam.Id, new Actor("other-teacher", UserRole.Instructor)), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidRequest_ProducesExcelWithHeaderAndResultRow()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResult = new ExamResult("student-1", exam.Id).WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.SetUserInfo("student1@example.com", "Nguyễn Văn A");
        examResult.Finish(5m);
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 0, 1, It.IsAny<CancellationToken>())).ReturnsAsync([examResult]);
        var handler = new ExportExamResultsQueryHandler(examRepository.Object, examResultRepository.Object);

        var bytes = await handler.Handle(new ExportExamResultsQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("Họ tên", worksheet.Cell(1, 1).GetString());
        Assert.Equal("Nguyễn Văn A", worksheet.Cell(2, 1).GetString());
        Assert.Equal("student1@example.com", worksheet.Cell(2, 2).GetString());
        Assert.Equal("Đã nộp", worksheet.Cell(2, 7).GetString());
    }

    [Fact]
    public async Task Handle_InProgressResult_ShowsDashForScoreAndPassStatus()
    {
        var exam = CreateExam("teacher-1");
        var examRepository = new Mock<IExamRepository>();
        examRepository.Setup(r => r.GetByIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(exam);
        var examResult = new ExamResult("student-1", exam.Id).WithId("result-1");
        examResult.AssignQuestions(["q-1"]);
        examResult.SetUserInfo("student1@example.com", "Nguyễn Văn A");
        var examResultRepository = new Mock<IExamResultRepository>();
        examResultRepository.Setup(r => r.CountByExamIdAsync(exam.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        examResultRepository.Setup(r => r.GetByExamIdAsync(exam.Id, 0, 1, It.IsAny<CancellationToken>())).ReturnsAsync([examResult]);
        var handler = new ExportExamResultsQueryHandler(examRepository.Object, examResultRepository.Object);

        var bytes = await handler.Handle(new ExportExamResultsQuery(exam.Id, new Actor("teacher-1", UserRole.Instructor)),
            CancellationToken.None);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var worksheet = workbook.Worksheets.First();
        Assert.Equal("-", worksheet.Cell(2, 3).GetString());
        Assert.Equal("-", worksheet.Cell(2, 4).GetString());
        Assert.Equal("Đang làm", worksheet.Cell(2, 7).GetString());
    }
}
