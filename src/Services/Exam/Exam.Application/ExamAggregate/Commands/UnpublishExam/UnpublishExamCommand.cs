using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UnpublishExam;

public record UnpublishExamCommand(string ExamId, Actor Actor) : IRequest<ExamDto>;
