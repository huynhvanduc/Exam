using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureNegativeMarking;

public record ConfigureNegativeMarkingCommand(string ExamId, decimal Ratio, Actor Actor) : IRequest<ExamDto>;
