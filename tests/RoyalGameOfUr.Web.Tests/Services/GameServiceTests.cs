using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class GameServiceTests
{
    [Test]
    public async Task StartGameAsync_InitializesState()
    {
        var svc = new GameService();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.OnChange = () =>
        {
            if (svc.State is not null) ready.TrySetResult();
            return Task.CompletedTask;
        };

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Human, "Bob");

        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(svc.State).IsNotNull();
        await Assert.That(svc.IsRunning).IsTrue();
        await Assert.That(svc.Rules.Name).IsEqualTo("Finkel");

        svc.StopGame();
    }

    [Test]
    public async Task StartGameAsync_SetsPlayerNames()
    {
        var svc = new GameService();

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Computer, "Bot");

        await Assert.That(svc.Player1Name).IsEqualTo("Alice");
        await Assert.That(svc.Player2Name).IsEqualTo("Bot");
        await Assert.That(svc.Player1Type).IsEqualTo(PlayerType.Human);
        await Assert.That(svc.Player2Type).IsEqualTo(PlayerType.Computer);

        svc.StopGame();
    }

    [Test]
    public async Task StopGame_CleansUp()
    {
        var svc = new GameService();
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        svc.OnChange = () =>
        {
            if (svc.State is not null) ready.TrySetResult();
            return Task.CompletedTask;
        };

        await svc.StartGameAsync(
            GameRules.Finkel,
            PlayerType.Human, "Alice",
            PlayerType.Human, "Bob");

        await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));

        svc.StopGame();

        await Assert.That(svc.IsRunning).IsFalse();
    }

    [Test]
    public async Task OnDiceRolled_SetsStatusMessage()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        var state = new GameStateBuilder(GameRules.Finkel).Build();
        await observer.OnStateChangedAsync(state);

        await observer.OnDiceRolledAsync(Player.One, 3);

        await Assert.That(svc.LastRollDisplay).IsEqualTo(3);
        await Assert.That(svc.DiceRolled).IsTrue();
        await Assert.That(svc.IndividualDice).IsNotNull();
        await Assert.That(svc.StatusMessage!).Contains("rolled 3");
    }

    [Test]
    public async Task OnMoveMade_ClearsValidMoves()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        var move = new Move(Player.One, 0, -1, 3);
        var outcome = new MoveOutcome(MoveResult.Moved);

        await observer.OnMoveMadeAsync(move, outcome);

        await Assert.That(svc.ValidMoves).IsEmpty();
        await Assert.That(svc.DiceRolled).IsFalse();
    }

    [Test]
    public async Task OnGameOver_SetsWinner()
    {
        var svc = new GameService();
        IGameObserver observer = svc;

        await observer.OnGameOverAsync(Player.Two);

        await Assert.That(svc.Winner).IsEqualTo(Player.Two);
        await Assert.That(svc.StatusMessage!).Contains("wins");
    }
}
