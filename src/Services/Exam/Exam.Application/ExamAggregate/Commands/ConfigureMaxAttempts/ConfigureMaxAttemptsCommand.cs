using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ConfigureMaxAttempts;

public record ConfigureMaxAttemptsCommand(string ExamId, int? MaxAttempts, Actor Actor) : IRequest<ExamDto>;
