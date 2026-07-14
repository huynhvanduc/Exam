using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.AdminForceFinishExam;

public record AdminForceFinishExamCommand(string ExamResultId) : IRequest<ExamResultDto>;
