using MediatR;

namespace Exam.Application.ExamAggregate.Queries.GetExamById;

public record GetExamByIdQuery(string Id) : IRequest<ExamDto?>;
