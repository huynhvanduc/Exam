using MediatR;

namespace Exam.Application.QuestionAggregate.Queries.ExportQuestions;

public record ExportQuestionsQuery(string CategoryId) : IRequest<byte[]>;
