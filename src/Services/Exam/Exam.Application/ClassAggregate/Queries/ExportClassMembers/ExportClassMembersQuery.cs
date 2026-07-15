using Exam.Application.Common;
using MediatR;

namespace Exam.Application.ClassAggregate.Queries.ExportClassMembers;

public record ExportClassMembersQuery(string ClassId, Actor Actor) : IRequest<byte[]>;
