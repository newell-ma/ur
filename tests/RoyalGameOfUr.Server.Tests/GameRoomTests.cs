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
}
