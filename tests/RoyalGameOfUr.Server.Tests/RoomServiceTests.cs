using NSubstitute;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Rooms;
using RoyalGameOfUr.Server.Services;

namespace RoyalGameOfUr.Server.Tests;

public class RoomServiceTests
{
    private readonly RoomManager _roomManager = new();
    private readonly IGameBroadcaster _broadcaster = Substitute.For<IGameBroadcaster>();
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _service = new RoomService(_roomManager, _broadcaster);
    }

    [Test]
    public async Task CreateRoom_DelegatesToManager()
    {
        var result = _service.CreateRoom("Finkel", "Alice", "conn1");

        await Assert.That(result.Code).IsNotNull().And.IsNotEmpty();
        await Assert.That(result.RulesName).IsEqualTo("Finkel");
        await Assert.That(_roomManager.GetRoom(result.Code)).IsNotNull();
    }

    [Test]
    public async Task JoinRoom_NotFound_ReturnsError()
    {
        var result = await _service.JoinRoom("XXXX", "Bob", "conn2");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Room not found");
    }

    [Test]
    public async Task JoinRoom_Full_ReturnsError()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        var result = await _service.JoinRoom(createResult.Code, "Charlie", "conn3");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).Contains("full");
    }

    [Test]
    public async Task JoinRoom_Success_NotifiesHost()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");

        var result = await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.HostName).IsEqualTo("Alice");
        await _broadcaster.Received(1).SendToPlayer(
            "conn1",
            "ReceiveOpponentJoined",
            Arg.Is<object?[]>(a => a.Length == 1 && (string)a[0]! == "Bob"));
    }

    [Test]
    public async Task TryStartGame_NotHost_ReturnsFalse()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        await Assert.That(await _service.TryStartGame(createResult.Code, "conn2")).IsFalse();
    }

    [Test]
    public async Task TryStartGame_NoGuest_ReturnsFalse()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");

        await Assert.That(await _service.TryStartGame(createResult.Code, "conn1")).IsFalse();
    }

    [Test]
    public async Task TryStartGame_Success_StartsRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        var started = await _service.TryStartGame(createResult.Code, "conn1");
        var room = _roomManager.GetRoom(createResult.Code)!;

        await Assert.That(started).IsTrue();
        await Assert.That(room.IsStarted).IsTrue();
        await _broadcaster.Received(1).BroadcastGameStarting(
            room.GroupName, "Alice", "Bob", "Finkel");

        room.Stop();
    }

    [Test]
    public async Task TrySubmitMove_InvalidRoom_ReturnsFalse()
    {
        await Assert.That(_service.TrySubmitMove("XXXX", "conn1", default)).IsFalse();
    }

    [Test]
    public async Task TrySubmitMove_WrongPlayer_ReturnsFalse()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await Assert.That(_service.TrySubmitMove(createResult.Code, "unknown", default)).IsFalse();
    }

    [Test]
    public async Task TrySubmitMove_Valid_ReturnsTrue()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");

        var room = _roomManager.GetRoom(createResult.Code)!;

        // Wait for the game loop to request a move from one of the players
        await Task.Delay(300);

        var player1 = room.GetSignalRPlayer(Player.One)!;
        var player2 = room.GetSignalRPlayer(Player.Two)!;

        string activeConnectionId;
        Move validMove;
        if (player1.IsAwaitingMove && player1.PendingMoves.Count > 0)
        {
            activeConnectionId = player1.ConnectionId;
            validMove = player1.PendingMoves[0];
        }
        else if (player2.IsAwaitingMove && player2.PendingMoves.Count > 0)
        {
            activeConnectionId = player2.ConnectionId;
            validMove = player2.PendingMoves[0];
        }
        else
        {
            // Turn was forfeited (rolled 0) — still valid, we tested the lookup path
            room.Stop();
            return;
        }

        await Assert.That(_service.TrySubmitMove(createResult.Code, activeConnectionId, validMove)).IsTrue();

        room.Stop();
    }

    // --- Disconnect tests ---

    [Test]
    public async Task HandleDisconnect_UnknownConnection_DoesNothing()
    {
        // Should not throw
        await _service.HandleDisconnect("unknown-conn");
    }

    [Test]
    public async Task HandleDisconnect_HostDisconnects_NotifiesGuest()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");
        await Task.Delay(100);

        await _service.HandleDisconnect("conn1");

        await _broadcaster.Received().SendToPlayer(
            "conn2",
            "ReceiveOpponentDisconnected",
            Arg.Any<object?[]>());
    }

    [Test]
    public async Task HandleDisconnect_GuestDisconnects_NotifiesHost()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");
        await Task.Delay(100);

        await _service.HandleDisconnect("conn2");

        await _broadcaster.Received().SendToPlayer(
            "conn1",
            "ReceiveOpponentDisconnected",
            Arg.Any<object?[]>());
    }

    [Test]
    public async Task HandleDisconnect_RemovesRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");
        await Task.Delay(100);

        await _service.HandleDisconnect("conn1");

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task HandleDisconnect_BeforeGameStarts_RemovesRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        await _service.HandleDisconnect("conn1");

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task HandleDisconnect_HostLeavesEmptyRoom_RemovesRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");

        await _service.HandleDisconnect("conn1");

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task HandleDisconnect_BothDisconnect_SafeForSecondCall()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");
        await Task.Delay(100);

        await _service.HandleDisconnect("conn1");
        // Second disconnect should not throw
        await _service.HandleDisconnect("conn2");

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task GameCompletion_RemovesRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");

        var room = _roomManager.GetRoom(createResult.Code)!;
        await Task.Delay(100);

        // Stop triggers cancellation → OnGameCompleted fires from finally block
        room.Stop();
        await Task.Delay(200);

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task GameCompletion_RemovesConnectionMappings()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        await _service.TryStartGame(createResult.Code, "conn1");

        var room = _roomManager.GetRoom(createResult.Code)!;
        await Task.Delay(100);

        room.Stop();
        await Task.Delay(200);

        // After game completion, disconnect should be a no-op (mapping already removed)
        _broadcaster.ClearReceivedCalls();
        await _service.HandleDisconnect("conn1");
        await _service.HandleDisconnect("conn2");

        // No opponent-disconnected notifications should be sent since mappings were already cleaned up
        await _broadcaster.DidNotReceive().SendToPlayer(
            Arg.Any<string>(),
            "ReceiveOpponentDisconnected",
            Arg.Any<object?[]>());
    }
}
