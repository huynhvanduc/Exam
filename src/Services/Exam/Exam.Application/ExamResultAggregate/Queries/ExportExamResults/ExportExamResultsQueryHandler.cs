using ClosedXML.Excel;
using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.ExamResultAggregate;
using MediatR;
using ExamEntity = Exam.Domain.AggregateModels.ExamAggregate.Exam;

namespace Exam.Application.ExamResultAggregate.Queries.ExportExamResults;

public class ExportExamResultsQueryHandler : IRequestHandler<ExportExamResultsQuery, byte[]>
{
    private static readonly string[] Headers = ["Họ tên", "Email", "Điểm", "Kết quả", "Bắt đầu", "Nộp bài", "Trạng thái"];

    private readonly IExamRepository _examRepository;
    private readonly IExamResultRepository _examResultRepository;

    public ExportExamResultsQueryHandler(IExamRepository examRepository, IExamResultRepository examResultRepository)
    {
        _examRepository = examRepository;
        _examResultRepository = examResultRepository;
    }

    public async Task<byte[]> Handle(ExportExamResultsQuery request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ExamEntity), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(ExamEntity), exam.Id);

        var total = await _examResultRepository.CountByExamIdAsync(request.ExamId, cancellationToken);
        var results = await _examResultRepository.GetByExamIdAsync(request.ExamId, 0, (int)total, cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ket qua");

        for (var i = 0; i < Headers.Length; i++)
            worksheet.Cell(1, i + 1).Value = Headers[i];
        worksheet.Row(1).Style.Font.Bold = true;

        var rowNumber = 2;
        foreach (var result in results.OrderByDescending(r => r.ExamStartDate))
        {
            worksheet.Cell(rowNumber, 1).Value = result.FullName;
            worksheet.Cell(rowNumber, 2).Value = result.Email;
            worksheet.Cell(rowNumber, 3).Value = result.Finished ? $"{result.TotalScore}/{result.MaxPossibleScore}" : "-";
            worksheet.Cell(rowNumber, 4).Value = result.Passed switch { true => "Đạt", false => "Không đạt", null => "-" };
            worksheet.Cell(rowNumber, 5).Value = result.ExamStartDate.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
            worksheet.Cell(rowNumber, 6).Value = result.ExamFinishDate.HasValue
                ? result.ExamFinishDate.Value.ToLocalTime().ToString("HH:mm dd/MM/yyyy")
                : "-";
            worksheet.Cell(rowNumber, 7).Value = result.Finished ? "Đã nộp" : "Đang làm";
            rowNumber++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
