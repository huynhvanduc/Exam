using Exam.Contracts;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureExamComposition;

public record ConfigureExamCompositionCommand(string ExamId, IReadOnlyCollection<ExamCompositionCellDto> Cells, Actor Actor) : IRequest<ExamDto>;
