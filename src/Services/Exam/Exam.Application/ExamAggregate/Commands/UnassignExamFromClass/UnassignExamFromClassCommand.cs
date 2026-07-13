using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UnassignExamFromClass;

public record UnassignExamFromClassCommand(string ExamId, string ClassId, Actor Actor) : IRequest<ExamDto>;
