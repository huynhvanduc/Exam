namespace Exam.Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }

    public static ForbiddenException NotOwner(string entityName, string id) =>
        new($"You do not own {entityName} '{id}' and do not have permission to modify it.");
}
