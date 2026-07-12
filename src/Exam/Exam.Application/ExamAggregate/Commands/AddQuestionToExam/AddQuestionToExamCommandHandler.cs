using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ExamAggregate;
using Exam.Domain.AggregateModels.QuestionAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.AddQuestionToExam;

public class AddQuestionToExamCommandHandler : IRequestHandler<AddQuestionToExamCommand, ExamDto>
{
    private readonly IExamRepository _examRepository;
    private readonly IQuestionRepository _questionRepository;

    public AddQuestionToExamCommandHandler(IExamRepository examRepository, IQuestionRepository questionRepository)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
    }

    public async Task<ExamDto> Handle(AddQuestionToExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        var question = await _questionRepository.GetByIdAsync(request.QuestionId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Question), request.QuestionId);

        exam.AddQuestion(question.Id);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
