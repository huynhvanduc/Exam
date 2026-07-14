using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public class ConfigureExamCompositionCommandHandler : IRequestHandler<ConfigureExamCompositionCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;

    public ConfigureExamCompositionCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto> Handle(ConfigureExamCompositionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        // Không kiểm tra số câu thực có trong ngân hàng ở đây - lỗi thiếu câu theo từng ô được nêu rõ khi
        // học viên bắt đầu làm bài (ExamQuestionPoolService.DrawQuestionIdsAsync), theo đúng spec.
        var cells = request.Cells
            .Select(c => new ExamCompositionCell(c.Level, c.QuestionType, c.Count))
            .ToList();

        exam.ConfigureComposition(cells);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
