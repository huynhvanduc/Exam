using MediatR;

namespace Exam.Application.ExamResultAggregate.Commands.RegradeExamResult;

public record RegradeExamResultCommand(string ExamResultId) : IRequest<ExamResultDto>;
