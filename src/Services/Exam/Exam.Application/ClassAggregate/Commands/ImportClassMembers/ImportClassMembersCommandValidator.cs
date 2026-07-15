using FluentValidation;

namespace Exam.Application.ClassAggregate.Commands.ImportClassMembers;

public class ImportClassMembersCommandValidator : AbstractValidator<ImportClassMembersCommand>
{
    public ImportClassMembersCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.FileContent).NotEmpty().WithMessage("File is required.");
    }
}
