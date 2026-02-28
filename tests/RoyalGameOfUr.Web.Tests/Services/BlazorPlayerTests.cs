using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class BlazorPlayerTests
{
    private static GameState CreateTestState()
    {
        return new GameStateBuilder(GameRules.Finkel).Build();
    }

    [Fact]
    public async Task ChooseMoveAsync_SetsAwaitingMove()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        Assert.False(player.IsAwaitingMove);

        var task = player.ChooseMoveAsync(state, moves, 4);

        // After calling, should be awaiting
        Assert.True(player.IsAwaitingMove);

        // Complete to avoid hanging
        player.SubmitMove(moves[0]);
        await task;
    }

    [Fact]
    public async Task SubmitMove_CompletesTask()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var expectedMove = new Move(Player.One, 0, -1, 3);
        var moves = new List<Move> { expectedMove };

        var task = player.ChooseMoveAsync(state, moves, 4);
        player.SubmitMove(expectedMove);

        var result = await task;
        Assert.Equal(expectedMove, result);
    }

    [Fact]
    public async Task ShouldSkipAsync_SetsAwaitingSkip()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        Assert.False(player.IsAwaitingSkip);

        var task = player.ShouldSkipAsync(state, moves, 4);

        Assert.True(player.IsAwaitingSkip);

        player.SubmitSkipDecision(false);
        await task;
    }

    [Fact]
    public async Task SubmitSkipDecision_CompletesTask()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ShouldSkipAsync(state, moves, 4);
        player.SubmitSkipDecision(true);

        var result = await task;
        Assert.True(result);
    }

    [Fact]
    public async Task Cancel_CancelsPendingMove()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ChooseMoveAsync(state, moves, 4);
        player.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
    public async Task Cancel_CancelsPendingSkip()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ShouldSkipAsync(state, moves, 4);
        player.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    [Fact]
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

        Assert.Equal(moves, player.PendingMoves);

        player.SubmitMove(moves[0]);
        await task;

        // After completion, PendingMoves should be cleared
        Assert.Empty(player.PendingMoves);
    }

    [Fact]
    public async Task PendingRoll_StoredDuringAwait()
    {
        var player = new BlazorPlayer("Test");
        var state = CreateTestState();
        var moves = new List<Move> { new(Player.One, 0, -1, 3) };

        var task = player.ChooseMoveAsync(state, moves, 3);

        Assert.Equal(3, player.PendingRoll);

        player.SubmitMove(moves[0]);
        await task;
    }
}
