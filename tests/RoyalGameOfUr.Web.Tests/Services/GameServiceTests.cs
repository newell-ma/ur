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
    public void OnDiceRolled_SetsStatusMessage()
    {
        var svc = new GameService();
        // Access the observer interface explicitly
        IGameObserver observer = svc;

        // Set up state so the observer can read EffectiveRoll
        // We need to set player names first
        // Use reflection-free approach: call StartGameAsync then test observer
        // Simpler: just test that calling OnDiceRolled updates the properties

        // Create a minimal state for the service
        var state = new GameStateBuilder(GameRules.Finkel).Build();
        observer.OnStateChanged(state);

        observer.OnDiceRolled(Player.One, 3);

        Assert.Equal(3, svc.LastRollDisplay);
        Assert.True(svc.DiceRolled);
        Assert.NotNull(svc.IndividualDice);
        Assert.Contains("rolled 3", svc.StatusMessage);
    }

    [Fact]
    public void OnMoveMade_ClearsValidMoves()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        var move = new Move(Player.One, 0, -1, 3);
        var outcome = new MoveOutcome(MoveResult.Moved);

        observer.OnMoveMade(move, outcome);

        Assert.Empty(svc.ValidMoves);
        Assert.False(svc.DiceRolled);
    }

    [Fact]
    public void OnGameOver_SetsWinner()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        observer.OnGameOver(Player.Two);

        Assert.Equal(Player.Two, svc.Winner);
        Assert.Contains("wins", svc.StatusMessage);
    }
}
