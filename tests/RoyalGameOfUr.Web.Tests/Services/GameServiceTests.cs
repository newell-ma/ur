using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class GameServiceTests
{
    [Fact]
    public async Task StartGameAsync_InitializesState()
    {
        var svc = new GameService();

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Human, "Bob");

        // Give the background runner a moment to fire OnStateChanged
        await Task.Delay(200);

        Assert.NotNull(svc.State);
        Assert.True(svc.IsRunning);
        Assert.Equal("Finkel", svc.Rules.Name);

        svc.StopGame();
    }

    [Fact]
    public async Task StartGameAsync_SetsPlayerNames()
    {
        var svc = new GameService();

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Computer, "Bot");

        Assert.Equal("Alice", svc.Player1Name);
        Assert.Equal("Bot", svc.Player2Name);
        Assert.Equal(PlayerType.Human, svc.Player1Type);
        Assert.Equal(PlayerType.Computer, svc.Player2Type);

        svc.StopGame();
    }

    [Fact]
    public async Task StopGame_CleansUp()
    {
        var svc = new GameService();

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Human, "Bob");

        await Task.Delay(100);

        svc.StopGame();

        Assert.False(svc.IsRunning);
    }

    [Fact]
    public async Task OnDiceRolled_SetsStatusMessage()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        var state = new GameStateBuilder(GameRules.Finkel).Build();
        await observer.OnStateChangedAsync(state);

        await observer.OnDiceRolledAsync(Player.One, 3);

        Assert.Equal(3, svc.LastRollDisplay);
        Assert.True(svc.DiceRolled);
        Assert.NotNull(svc.IndividualDice);
        Assert.Contains("rolled 3", svc.StatusMessage);
    }

    [Fact]
    public async Task OnMoveMade_ClearsValidMoves()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        var move = new Move(Player.One, 0, -1, 3);
        var outcome = new MoveOutcome(MoveResult.Moved);

        await observer.OnMoveMadeAsync(move, outcome);

        Assert.Empty(svc.ValidMoves);
        Assert.False(svc.DiceRolled);
    }

    [Fact]
    public async Task OnGameOver_SetsWinner()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        await observer.OnGameOverAsync(Player.Two);

        Assert.Equal(Player.Two, svc.Winner);
        Assert.Contains("wins", svc.StatusMessage);
    }
}
