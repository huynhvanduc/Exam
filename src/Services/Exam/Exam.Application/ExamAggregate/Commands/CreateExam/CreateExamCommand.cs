using Exam.Contracts;
using MediatR;

namespace Exam.Application.ExamAggregate.Commands.CreateExam;

public record CreateExamCommand(
    string Name,
    string ShortDesc,
    string Content,
    TimeSpan Duration,
    Level Level,
    string CategoryId,
    bool IsTimeRestricted,
    int MinimumPassingScore,
    string OwnerUserId) : IRequest<ExamDto>;
