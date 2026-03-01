using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class BlazorPlayerTests
{
    private static GameState CreateTestState()
    {
        return new GameStateBuilder(GameRules.Finkel).Build();
    }

    [Test]
    public async Task ChooseMoveAsync_SetsAwaitingMove()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        await Assert.That(player.IsAwaitingMove).IsFalse();

        var task = player.ChooseMoveAsync(state, moves, 4);

        await Assert.That(player.IsAwaitingMove).IsTrue();

        player.SubmitMove(moves[0]);
        await task;
    }

    [Test]
    public async Task SubmitMove_CompletesTask()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var expectedMove = new Move(Player.One, 0, -1, 3);
        var moves = new List<Move> { expectedMove };

        var task = player.ChooseMoveAsync(state, moves, 4);
        player.SubmitMove(expectedMove);

        var result = await task;
        await Assert.That(result).IsEqualTo(expectedMove);
    }

    [Test]
    public async Task ShouldSkipAsync_SetsAwaitingSkip()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        await Assert.That(player.IsAwaitingSkip).IsFalse();

        var task = player.ShouldSkipAsync(state, moves, 4);

        await Assert.That(player.IsAwaitingSkip).IsTrue();

        player.SubmitSkipDecision(false);
        await task;
    }

    [Test]
    public async Task SubmitSkipDecision_CompletesTask()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ShouldSkipAsync(state, moves, 4);
        player.SubmitSkipDecision(true);

        var result = await task;
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Cancel_CancelsPendingMove()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ChooseMoveAsync(state, moves, 4);
        player.Cancel();

        await Assert.That(async () => await task).ThrowsException();
    }

    [Test]
    public async Task Cancel_CancelsPendingSkip()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ShouldSkipAsync(state, moves, 4);
        player.Cancel();

        await Assert.That(async () => await task).ThrowsException();
    }

    [Test]
    public async Task PendingMoves_StoredDuringAwait()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move>
        {
            new(Player.One, 0, -1, 3),
            new(Player.One, 1, -1, 3)
        };

        var task = player.ChooseMoveAsync(state, moves, 4);

        await Assert.That(player.PendingMoves).IsEquivalentTo(moves);

        player.SubmitMove(moves[0]);
        await task;

        await Assert.That(player.PendingMoves).IsEmpty();
    }

    [Test]
    public async Task PendingRoll_StoredDuringAwait()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ChooseMoveAsync(state, moves, 3);

        await Assert.That(player.PendingRoll).IsEqualTo(3);

        player.SubmitMove(moves[0]);
        await task;
    }
}
