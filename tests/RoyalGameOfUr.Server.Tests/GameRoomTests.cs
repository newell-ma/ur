using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Tests;

public class GameRoomTests
{
    [Test]
    public async Task TryJoin_Success()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        await Assert.That(room.TryJoin("Bob", "conn2")).IsTrue();
        await Assert.That(room.Player2).IsNotNull();
        await Assert.That(room.Player2!.Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task TryJoin_AlreadyFull_ReturnsFalse()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        await Assert.That(room.TryJoin("Charlie", "conn3")).IsFalse();
    }

    [Test]
    public async Task TryJoin_AlreadyStarted_ReturnsFalse()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        var broadcaster = Substitute.For<IGameBroadcaster>();
        room.Start(broadcaster);

        await Assert.That(room.TryJoin("Charlie", "conn3")).IsFalse();

        room.Stop();
    }

    [Test]
    public async Task TryJoin_ConcurrentJoins_OnlyOneSucceeds()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");

        var results = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(i =>
                Task.Run(() => room.TryJoin($"Guest{i}", $"guest-conn-{i}"))));

        var successCount = results.Count(r => r);
        await Assert.That(successCount).IsEqualTo(1);
        await Assert.That(room.Player2).IsNotNull();
    }

    [Test]
    public async Task GetPlayerSide_HostIsPlayerOne()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        await Assert.That(room.GetPlayerSide("conn1")).IsEqualTo(Player.One);
    }

    [Test]
    public async Task GetPlayerSide_GuestIsPlayerTwo()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        await Assert.That(room.GetPlayerSide("conn2")).IsEqualTo(Player.Two);
    }

    [Test]
    public async Task GetPlayerSide_UnknownConnection_ReturnsNull()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        await Assert.That(room.GetPlayerSide("unknown")).IsNull();
    }

    [Test]
    public async Task Start_BroadcastsStateChanged()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        await broadcaster.Received().BroadcastStateChanged(
            room.GroupName,
            Arg.Any<GameStateDto>());

        room.Stop();
    }

    [Test]
    public async Task OnDiceRolled_BroadcastsThroughBroadcaster()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        broadcaster.ClearReceivedCalls();

        var observer = (IGameObserver)room;
        await observer.OnDiceRolledAsync(Player.One, 3);

        await broadcaster.Received(1).BroadcastDiceRolled(room.GroupName, Player.One, 3);

        room.Stop();
    }

    [Test]
    public async Task OnMoveMade_BroadcastsThroughBroadcaster()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        broadcaster.ClearReceivedCalls();

        var move = new Move(Player.One, 0, -1, 2);
        var outcome = new MoveOutcome(MoveResult.Moved);
        var observer = (IGameObserver)room;
        await observer.OnMoveMadeAsync(move, outcome);

        await broadcaster.Received(1).BroadcastMoveMade(room.GroupName, move, outcome);

        room.Stop();
    }

    [Test]
    public async Task OnGameOver_SetsFinished_And_Broadcasts()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        broadcaster.ClearReceivedCalls();

        var observer = (IGameObserver)room;
        await observer.OnGameOverAsync(Player.One);

        await Assert.That(room.IsFinished).IsTrue();
        await broadcaster.Received(1).BroadcastGameOver(room.GroupName, Player.One);

        room.Stop();
    }

    [Test]
    public async Task OnGameCompleted_FiredAfterStop()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        string? completedCode = null;
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        room.OnGameCompleted = code => { completedCode = code; completed.TrySetResult(); };

        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        room.Stop();
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(completedCode).IsEqualTo("TEST");
    }

    [Test]
    public async Task Start_Timeout_NotifiesOpponent()
    {
        var fakeTime = new FakeTimeProvider();
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var moveRequested = WhenMoveRequested(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1", fakeTime);
        room.TryJoin("Bob", "conn2");

        room.Start(broadcaster);
        await moveRequested.WaitAsync(TimeSpan.FromSeconds(5));

        // Advance past the 60s move timeout
        fakeTime.Advance(TimeSpan.FromSeconds(60));

        // One of the players timed out — opponent should be notified
        await broadcaster.Received().SendToPlayer(
            Arg.Any<string>(),
            "ReceiveOpponentSlow",
            Arg.Any<object?[]>());

        room.Stop();
    }

    [Test]
    public async Task Start_DeliberateStop_NoBroadcastError()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        room.Stop();
        await WaitUntilAsync(() => room.IsFinished);

        await broadcaster.DidNotReceive().BroadcastError(Arg.Any<string>(), Arg.Any<string>());
    }

    // --- Session token tests ---

    [Test]
    public async Task Constructor_SetsPlayer1Token()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        await Assert.That(room.Player1Token).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task TryJoin_SetsPlayer2Token()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        await Assert.That(room.Player2Token).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task GetPlayerByToken_ReturnsCorrectPlayer()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        var result1 = room.GetPlayerByToken(room.Player1Token!);
        await Assert.That(result1).IsNotNull();
        await Assert.That(result1!.Value.Player.Name).IsEqualTo("Alice");
        await Assert.That(result1!.Value.Side).IsEqualTo(Player.One);

        var result2 = room.GetPlayerByToken(room.Player2Token!);
        await Assert.That(result2).IsNotNull();
        await Assert.That(result2!.Value.Player.Name).IsEqualTo("Bob");
        await Assert.That(result2!.Value.Side).IsEqualTo(Player.Two);
    }

    [Test]
    public async Task GetPlayerByToken_InvalidToken_ReturnsNull()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        await Assert.That(room.GetPlayerByToken("bogus")).IsNull();
    }

    // --- Grace period tests ---

    [Test]
    public async Task StartGracePeriod_ExpiresAfterTimeout_FiresCallback()
    {
        var fakeTime = new FakeTimeProvider();
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1", fakeTime);

        string? firedCode = null;
        string? firedConnId = null;
        room.OnGracePeriodExpired = (code, connId) =>
        {
            firedCode = code;
            firedConnId = connId;
        };

        room.StartGracePeriod("conn1");
        fakeTime.Advance(TimeSpan.FromSeconds(30));

        await Assert.That(firedCode).IsEqualTo("TEST");
        await Assert.That(firedConnId).IsEqualTo("conn1");
    }

    [Test]
    public async Task CancelGracePeriod_BeforeExpiry_PreventsCallback()
    {
        var fakeTime = new FakeTimeProvider();
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1", fakeTime);

        bool fired = false;
        room.OnGracePeriodExpired = (_, _) => fired = true;

        room.StartGracePeriod("conn1");
        var cancelled = room.CancelGracePeriod("conn1");

        fakeTime.Advance(TimeSpan.FromSeconds(30));

        await Assert.That(cancelled).IsTrue();
        await Assert.That(fired).IsFalse();
    }

    [Test]
    public async Task CancelGracePeriod_WrongConnectionId_ReturnsFalse()
    {
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.GracePeriod = TimeSpan.FromSeconds(30);

        room.StartGracePeriod("conn1");
        var cancelled = room.CancelGracePeriod("conn-other");

        await Assert.That(cancelled).IsFalse();

        room.Stop(); // clean up
    }

    // --- LastStateDto cache test ---

    [Test]
    public async Task LastStateDto_CachedOnStateChanged()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var gameLoopReady = WhenGameLoopReady(broadcaster);
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);
        await gameLoopReady.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(room.LastStateDto).IsNotNull();

        room.Stop();
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
