using Exam.Application.ClassAggregate.Commands.CreateClassRoom;
using Exam.Domain.AggregateModels.ClassAggregate;

namespace Exam.Application.UnitTests.ClassAggregate.Commands;

public class CreateClassRoomCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidRequest_InsertsClassRoomAndReturnsDto()
    {
        var repository = new Mock<IClassRoomRepository>();
        var handler = new CreateClassRoomCommandHandler(repository.Object);

        var result = await handler.Handle(new CreateClassRoomCommand("Lớp 10A1", "teacher-1"), CancellationToken.None);

        Assert.Equal("Lớp 10A1", result.Name);
        Assert.Equal("teacher-1", result.OwnerUserId);
        Assert.Equal(0, result.MemberCount);
        repository.Verify(r => r.InsertAsync(It.Is<ClassRoom>(c => c.Name == "Lớp 10A1"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CreateClassRoomCommandValidatorTests
{
    public static IEnumerable<object?[]> Cases() => CsvTestData.Read("CreateClassRoomCommandValidator.csv",
        CsvTestData.Str, CsvTestData.Str, CsvTestData.Bool);

    [Theory]
    [MemberData(nameof(Cases))]
    public void Validate(string name, string ownerUserId, bool isValid)
    {
        var validator = new CreateClassRoomCommandValidator();

        var result = validator.Validate(new CreateClassRoomCommand(name, ownerUserId));

        Assert.Equal(isValid, result.IsValid);
    }
}
