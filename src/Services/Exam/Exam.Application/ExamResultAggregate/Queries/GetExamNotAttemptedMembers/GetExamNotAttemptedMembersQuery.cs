using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ExamResultAggregate.Queries.GetExamNotAttemptedMembers;

public record GetExamNotAttemptedMembersQuery(string ExamId, Actor Actor) : IRequest<IReadOnlyCollection<ClassMemberDto>>;
