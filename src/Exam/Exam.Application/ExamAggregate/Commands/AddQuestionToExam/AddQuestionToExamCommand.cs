using MediatR;

namespace Exam.Application.ExamAggregate.Commands.AddQuestionToExam;

public record AddQuestionToExamCommand(string ExamId, string QuestionId) : IRequest<ExamDto>;
