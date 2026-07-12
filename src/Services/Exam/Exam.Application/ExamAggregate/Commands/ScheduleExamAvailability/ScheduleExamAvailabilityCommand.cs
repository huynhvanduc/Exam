using MediatR;

namespace Exam.Application.ExamAggregate.Commands.ScheduleExamAvailability;

public record ScheduleExamAvailabilityCommand(string ExamId, DateTime? AvailableFrom, DateTime? AvailableTo) : IRequest<ExamDto>;
