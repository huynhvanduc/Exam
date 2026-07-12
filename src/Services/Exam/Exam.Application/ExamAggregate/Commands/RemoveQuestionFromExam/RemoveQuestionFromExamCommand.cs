using MediatR;

namespace Exam.Application.ExamAggregate.Commands.RemoveQuestionFromExam;

public record RemoveQuestionFromExamCommand(string ExamId, string QuestionId, Actor Actor) : IRequest<ExamDto>;
