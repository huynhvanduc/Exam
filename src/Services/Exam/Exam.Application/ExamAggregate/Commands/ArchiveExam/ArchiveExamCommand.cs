using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ArchiveExam;

public record ArchiveExamCommand(string ExamId, Actor Actor) : IRequest<ExamDto>;
