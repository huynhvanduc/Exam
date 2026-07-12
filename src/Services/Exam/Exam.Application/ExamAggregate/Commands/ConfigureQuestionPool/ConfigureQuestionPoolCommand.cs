using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureQuestionPool;

public record ConfigureQuestionPoolCommand(string ExamId, string PoolCategoryId, int PoolQuestionCount, Actor Actor) : IRequest<ExamDto>;
