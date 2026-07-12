namespace Exam.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string entityName, string id) =>
        new($"{entityName} with id '{id}' was not found.");
}
