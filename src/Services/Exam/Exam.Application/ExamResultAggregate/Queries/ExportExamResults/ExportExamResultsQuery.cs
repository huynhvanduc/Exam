using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.ExportExamResults;

public record ExportExamResultsQuery(string ExamId, Actor Actor) : IRequest<byte[]>;
