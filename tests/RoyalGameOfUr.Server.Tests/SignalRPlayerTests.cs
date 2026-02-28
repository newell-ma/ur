using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Tests;

public class SignalRPlayerTests
{
    private static GameState CreateDummyState()
    {
        var rules = GameRules.Finkel;
        var builder = new GameStateBuilder(rules);
        return builder.Build();
    }

    [Test]
    public async Task ChooseMoveAsync_SetsAwaitingMove()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var moves = new[] { new Move(Player.One, 0, -1, 2) };

        var task = player.ChooseMoveAsync(state, moves, 2);

        await Assert.That(player.IsAwaitingMove).IsTrue();
        await Assert.That(player.PendingMoves).IsEquivalentTo(moves);

        player.TrySubmitMove(moves[0]);
        await task;
    }

    [Test]
    public async Task TrySubmitMove_ValidMove_ReturnsTrue()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var move = new Move(Player.One, 0, -1, 2);

        var task = player.ChooseMoveAsync(state, [move], 2);

        await Assert.That(player.TrySubmitMove(move)).IsTrue();
        var result = await task;
        await Assert.That(result).IsEqualTo(move);
    }

    [Test]
    public async Task TrySubmitMove_InvalidMove_ReturnsFalse()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var validMove = new Move(Player.One, 0, -1, 2);
        var invalidMove = new Move(Player.One, 1, -1, 3);

        var task = player.ChooseMoveAsync(state, [validMove], 2);

        await Assert.That(player.TrySubmitMove(invalidMove)).IsFalse();

        player.TrySubmitMove(validMove);
        await task;
    }

    [Test]
    public async Task TrySubmitMove_NotAwaiting_ReturnsFalse()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var move = new Move(Player.One, 0, -1, 2);

        await Assert.That(player.TrySubmitMove(move)).IsFalse();
    }

    [Test]
    public async Task ShouldSkipAsync_SetsAwaitingSkip()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var moves = new[] { new Move(Player.One, 0, -1, 2) };

        var task = player.ShouldSkipAsync(state, moves, 2);

        await Assert.That(player.IsAwaitingSkip).IsTrue();

        player.TrySubmitSkipDecision(false);
        await task;
    }

    [Test]
    public async Task TrySubmitSkipDecision_ReturnsTrue()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var moves = new[] { new Move(Player.One, 0, -1, 2) };

        var task = player.ShouldSkipAsync(state, moves, 2);

        await Assert.That(player.TrySubmitSkipDecision(true)).IsTrue();
        var result = await task;
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task TrySubmitSkipDecision_NotAwaiting_ReturnsFalse()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        await Assert.That(player.TrySubmitSkipDecision(true)).IsFalse();
    }

    [Test]
    public async Task Cancel_CancelsPending()
    {
        var player = new SignalRPlayer("Alice", "conn1");
        var state = CreateDummyState();
        var moves = new[] { new Move(Player.One, 0, -1, 2) };

        var task = player.ChooseMoveAsync(state, moves, 2);
        player.Cancel();

        await Assert.That(async () => await task).ThrowsException()
            .WithMessageMatching("*cancel*");
    }
}
