using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UpdateExam;

public class UpdateExamCommandHandler : IRequestHandler<UpdateExamCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;
    private readonly ICategoryRepository _categoryRepository;

    public UpdateExamCommandHandler(IExamRepository examRepository, ICategoryRepository categoryRepository)
    {
        _examRepository = examRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ExamDto> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.Id);

        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        exam.UpdateDetails(request.Name, request.ShortDesc, request.Content, request.Duration, request.Level,
            category.Id, category.Name, request.IsTimeRestricted, request.MinimumPassingScore);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
