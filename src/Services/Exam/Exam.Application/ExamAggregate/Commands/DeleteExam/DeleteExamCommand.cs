using MediatR;

namespace Exam.Application.ExamAggregate.Commands.DeleteExam;

public record DeleteExamCommand(string Id, Actor Actor) : IRequest;
