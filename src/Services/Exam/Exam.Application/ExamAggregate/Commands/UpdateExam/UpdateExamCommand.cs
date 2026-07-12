using Exam.Contracts;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.UpdateExam;

public record UpdateExamCommand(
    string Id,
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    int MinimumPassingScore) : IRequest<ExamDto>;
