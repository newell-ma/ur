using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Tests;

public class RoomManagerTests
{
    private readonly RoomManager _manager = new();

    [Test]
    public async Task CreateRoom_ReturnsUniqueCode()
    {
        var room1 = _manager.CreateRoom("Finkel", "Alice", "conn1");
        var room2 = _manager.CreateRoom("Finkel", "Bob", "conn2");
        await Assert.That(room1.Code).IsNotEqualTo(room2.Code);
    }

    [Test]
    public async Task CreateRoom_RoomIsRetrievable()
    {
        var room = _manager.CreateRoom("Finkel", "Alice", "conn1");
        var retrieved = _manager.GetRoom(room.Code);
        await Assert.That(retrieved).IsSameReferenceAs(room);
    }

    [Test]
    public async Task GetRoom_CaseInsensitive()
    {
        var room = _manager.CreateRoom("Finkel", "Alice", "conn1");
        var retrieved = _manager.GetRoom(room.Code.ToLowerInvariant());
        await Assert.That(retrieved).IsSameReferenceAs(room);
    }

    [Test]
    public async Task GetRoom_NotFound_ReturnsNull()
    {
        await Assert.That(_manager.GetRoom("ZZZZ")).IsNull();
    }

    [Test]
    public async Task RemoveRoom_RemovesRoom()
    {
        var room = _manager.CreateRoom("Finkel", "Alice", "conn1");
        await Assert.That(_manager.RemoveRoom(room.Code)).IsTrue();
        await Assert.That(_manager.GetRoom(room.Code)).IsNull();
    }

    [Test]
    public async Task CreateRoom_NoNullWindow()
    {
        var room = _manager.CreateRoom("Finkel", "Alice", "conn1");
        await Assert.That(_manager.GetRoom(room.Code)).IsNotNull();
    }
}
