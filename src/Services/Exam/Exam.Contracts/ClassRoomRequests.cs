namespace Exam.Contracts;

public record CreateClassRoomRequest(string Name);

public record RenameClassRoomRequest(string Name);

public record JoinClassRequest(string JoinCode);
