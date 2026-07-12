using MediatR;

namespace Exam.Application.ExamAggregate.Commands.PublishExam;

public record PublishExamCommand(string ExamId) : IRequest<ExamDto>;
