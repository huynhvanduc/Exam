using Exam.Application.Common;
using Exam.Contracts;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamAnalytics;

public record GetExamAnalyticsQuery(string ExamId, Actor Actor) : IRequest<ExamAnalyticsDto>;
