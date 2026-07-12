using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.CategoryAggregate;
using Exam.Domain.AggregateModels.ExamAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.CreateExam;

public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateExamCommandHandler(IExamRepository examRepository, ICategoryRepository categoryRepository)
    {
        _examRepository = examRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<ExamDto> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Category), request.CategoryId);

        var exam = new Domain.AggregateModels.ExamAggregate.Exam(request.Name, request.ShortDesc, request.Content,
            request.Duration, request.Level, request.OwnerUserId, category.Id, category.Name,
            request.IsTimeRestricted, request.MinimumPassingScore);

        await _examRepository.InsertAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
