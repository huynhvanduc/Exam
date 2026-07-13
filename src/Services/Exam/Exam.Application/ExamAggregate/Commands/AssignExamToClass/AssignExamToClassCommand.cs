using MediatR;

namespace Exam.Application.ExamAggregate.Commands.AssignExamToClass;

public record AssignExamToClassCommand(string ExamId, string ClassId, Actor Actor) : IRequest<ExamDto>;
