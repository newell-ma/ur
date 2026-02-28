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
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        room.Start(broadcaster);

        // Wait for the game loop to fire the initial state event
        await Task.Delay(200);

        await broadcaster.Received().BroadcastStateChanged(
            room.GroupName,
            Arg.Any<GameStateDto>());

        room.Stop();
    }

    [Test]
    public async Task OnDiceRolled_BroadcastsThroughBroadcaster()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);

        await Task.Delay(100);
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
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);

        await Task.Delay(100);
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
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");
        room.Start(broadcaster);

        await Task.Delay(100);
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
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        string? completedCode = null;
        room.OnGameCompleted = code => completedCode = code;

        room.Start(broadcaster);
        await Task.Delay(100);

        room.Stop();
        // Wait for Task.Run finally block to execute
        await Task.Delay(200);

        await Assert.That(completedCode).IsEqualTo("TEST");
    }

    [Test]
    public async Task Start_Timeout_NotifiesOpponent()
    {
        var broadcaster = Substitute.For<IGameBroadcaster>();
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        // Set very short timeout so the notification fires quickly
        room.Player1!.MoveTimeout = TimeSpan.FromMilliseconds(50);
        room.Player2!.MoveTimeout = TimeSpan.FromMilliseconds(50);

        room.Start(broadcaster);

        // Wait for the timeout notification to fire
        await Task.Delay(500);

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
        var room = new GameRoom("TEST", "Finkel", "Alice", "conn1");
        room.TryJoin("Bob", "conn2");

        room.Start(broadcaster);
        await Task.Delay(100);

        room.Stop();
        await Task.Delay(200);

        await broadcaster.DidNotReceive().BroadcastError(Arg.Any<string>(), Arg.Any<string>());
    }
}
