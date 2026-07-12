using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.GetQuestionById;

public record GetQuestionByIdQuery(string Id) : IRequest<QuestionDto?>;
