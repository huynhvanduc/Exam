using Exam.Application.Exceptions;
using Exam.Domain.AggregateModels.ClassAggregate;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.AssignExamToClass;

public class AssignExamToClassCommandHandler : IRequestHandler<AssignExamToClassCommand, ExamDto>
{
    private readonly Domain.AggregateModels.ExamAggregate.IExamRepository _examRepository;
    private readonly IClassRoomRepository _classRoomRepository;

    public AssignExamToClassCommandHandler(Domain.AggregateModels.ExamAggregate.IExamRepository examRepository,
        IClassRoomRepository classRoomRepository)
    {
        _examRepository = examRepository;
        _classRoomRepository = classRoomRepository;
    }

    public async Task<ExamDto> Handle(AssignExamToClassCommand request, CancellationToken cancellationToken)
    {
        var exam = await _examRepository.GetByIdAsync(request.ExamId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Domain.AggregateModels.ExamAggregate.Exam), request.ExamId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, exam.OwnerUserId, nameof(Domain.AggregateModels.ExamAggregate.Exam), exam.Id);

        var classRoom = await _classRoomRepository.GetByIdAsync(request.ClassId, cancellationToken)
            ?? throw NotFoundException.For(nameof(ClassRoom), request.ClassId);

        OwnershipGuard.EnsureOwnerOrAdmin(request.Actor, classRoom.OwnerUserId, nameof(ClassRoom), classRoom.Id);

        exam.AssignToClass(request.ClassId);

        await _examRepository.UpdateAsync(exam, cancellationToken);

        return ExamMapper.ToDto(exam);
    }
}
