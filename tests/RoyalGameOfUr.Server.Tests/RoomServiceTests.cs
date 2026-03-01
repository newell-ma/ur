using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;
using RoyalGameOfUr.Server.Rooms;
using RoyalGameOfUr.Server.Services;

namespace RoyalGameOfUr.Server.Tests;

public class RoomServiceTests
{
    private readonly FakeTimeProvider _fakeTime = new();
    private readonly RoomManager _roomManager;
    private readonly IGameBroadcaster _broadcaster = Substitute.For<IGameBroadcaster>();
    private readonly ILogger<RoomService> _logger = Substitute.For<ILogger<RoomService>>();
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _roomManager = new RoomManager(_fakeTime);
        _service = new RoomService(_roomManager, _broadcaster, _logger);
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
    public async Task CreateRoom_ReturnsSessionToken()
    {
        var result = _service.CreateRoom("Finkel", "Alice", "conn1");

        await Assert.That(result.SessionToken).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task CreateRoom_EmptyName_ReturnsError()
    {
        var result = _service.CreateRoom("Finkel", "", "conn1");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Player name is required");
    }

    [Test]
    public async Task CreateRoom_WhitespaceName_ReturnsError()
    {
        var result = _service.CreateRoom("Finkel", "   ", "conn1");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Player name is required");
    }

    [Test]
    public async Task CreateRoom_TooLongName_ReturnsError()
    {
        var result = _service.CreateRoom("Finkel", new string('A', 21), "conn1");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).Contains("20 characters or fewer");
    }

    [Test]
    public async Task JoinRoom_EmptyName_ReturnsError()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");

        var result = await _service.JoinRoom(createResult.Code, "", "conn2");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsEqualTo("Player name is required");
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
    public async Task JoinRoom_Success_ReturnsSessionToken()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");

        var result = await _service.JoinRoom(createResult.Code, "Bob", "conn2");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(result.SessionToken).IsNotNull().And.IsNotEmpty();
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

        var moveRequested = WhenMoveRequested(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await moveRequested.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;
        var player1 = room.GetSignalRPlayer(Player.One)!;
        var player2 = room.GetSignalRPlayer(Player.Two)!;

        var (activeConnectionId, validMove) = player1.IsAwaitingMove
            ? (player1.ConnectionId, player1.PendingMoves[0])
            : (player2.ConnectionId, player2.PendingMoves[0]);

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
    public async Task HandleDisconnect_DuringGame_StartsGracePeriod_NotImmediateTeardown()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        await _service.HandleDisconnect("conn1");

        // Room should still exist (grace period active, not torn down yet)
        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNotNull();

        // Opponent notified with "reconnecting", NOT "disconnected"
        await _broadcaster.Received().SendToPlayer(
            "conn2",
            "ReceiveOpponentReconnecting",
            Arg.Any<object?[]>());

        _roomManager.GetRoom(createResult.Code)!.Stop();
    }

    [Test]
    public async Task HandleDisconnect_BeforeGameStarts_ImmediateTeardown()
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
    public async Task GracePeriodExpired_TearsDownRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        await _service.HandleDisconnect("conn1");

        // Advance past the 30s grace period
        _fakeTime.Advance(TimeSpan.FromSeconds(30));

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task GracePeriodExpired_NotifiesOpponentDisconnected()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));
        _broadcaster.ClearReceivedCalls();

        await _service.HandleDisconnect("conn1");

        // Advance past the 30s grace period
        _fakeTime.Advance(TimeSpan.FromSeconds(30));

        await _broadcaster.Received().SendToPlayer(
            "conn2",
            "ReceiveOpponentDisconnected",
            Arg.Any<object?[]>());
    }

    // --- Rejoin tests ---

    [Test]
    public async Task HandleRejoin_ValidToken_SwapsConnectionId()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;

        await _service.HandleDisconnect("conn1");

        var result = await _service.HandleRejoin(createResult.SessionToken, "conn1-new");

        await Assert.That(result.Success).IsTrue();
        await Assert.That(room.Player1!.ConnectionId).IsEqualTo("conn1-new");

        room.Stop();
    }

    [Test]
    public async Task HandleRejoin_ValidToken_CancelsGracePeriod()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;

        await _service.HandleDisconnect("conn1");

        // Rejoin before grace expires
        await _service.HandleRejoin(createResult.SessionToken, "conn1-new");

        // Advance past grace period — room should still exist because grace was cancelled
        _fakeTime.Advance(TimeSpan.FromSeconds(30));

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNotNull();

        room.Stop();
    }

    [Test]
    public async Task HandleRejoin_InvalidToken_ReturnsFailure()
    {
        var result = await _service.HandleRejoin("bogus-token", "conn-new");

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).Contains("Invalid session token");
    }

    [Test]
    public async Task HandleRejoin_NotifiesOpponentReconnected()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;
        await _service.HandleDisconnect("conn1");
        _broadcaster.ClearReceivedCalls();

        await _service.HandleRejoin(createResult.SessionToken, "conn1-new");

        await _broadcaster.Received().SendToPlayer(
            "conn2",
            "ReceiveOpponentReconnected",
            Arg.Any<object?[]>());

        room.Stop();
    }

    [Test]
    public async Task GameCompletion_RemovesRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;

        // Stop triggers cancellation -> OnGameCompleted fires from finally block
        room.Stop();
        await WaitUntilAsync(() => _roomManager.GetRoom(createResult.Code) is null);

        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    [Test]
    public async Task GameCompletion_RemovesConnectionMappings()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        var room = _roomManager.GetRoom(createResult.Code)!;

        room.Stop();
        await WaitUntilAsync(() => _roomManager.GetRoom(createResult.Code) is null);

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

    [Test]
    public async Task GracePeriodExpired_SendFails_StillCleansUpRoom()
    {
        var createResult = _service.CreateRoom("Finkel", "Alice", "conn1");
        await _service.JoinRoom(createResult.Code, "Bob", "conn2");
        var gameLoopReady = WhenGameLoopReady(_broadcaster);
        await _service.TryStartGame(createResult.Code, "conn1");
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        // Disconnect first (sends ReceiveOpponentReconnecting successfully)
        await _service.HandleDisconnect("conn1");

        // Now configure broadcaster to throw on future SendToPlayer calls
        // (simulates conn2 dying before grace period expires)
        _broadcaster.SendToPlayer(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object?[]>())
            .ThrowsAsync(new InvalidOperationException("Connection is dead"));

        // Advance past the 30s grace period
        _fakeTime.Advance(TimeSpan.FromSeconds(30));

        // Room should still be cleaned up despite the send failure
        await Assert.That(_roomManager.GetRoom(createResult.Code)).IsNull();
    }

    // --- Helpers ---

    private static Task WhenGameLoopReady(IGameBroadcaster broadcaster)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        broadcaster.When(b => b.BroadcastStateChanged(Arg.Any<string>(), Arg.Any<GameStateDto>()))
            .Do(_ => tcs.TrySetResult());
        return tcs.Task;
    }

    private static Task WhenMoveRequested(IGameBroadcaster broadcaster)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        broadcaster.When(b => b.SendToPlayer(Arg.Any<string>(), "ReceiveMoveRequired", Arg.Any<object?[]>()))
            .Do(_ => tcs.TrySetResult());
        return tcs.Task;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(1, cts.Token);
    }
}
