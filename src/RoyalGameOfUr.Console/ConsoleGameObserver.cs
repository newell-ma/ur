using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public sealed class ConsoleGameObserver : IGameObserver
{
    private readonly IPlayer _playerOne;
    private readonly IPlayer _playerTwo;
    private readonly GameRules _rules;

    public ConsoleGameObserver(IPlayer playerOne, IPlayer playerTwo, GameRules rules)
    {
        _playerOne = playerOne;
        _playerTwo = playerTwo;
        _rules = rules;
    }

    public Task OnStateChangedAsync(GameState state)
    {
        BoardRenderer.Render(state);
        return Task.CompletedTask;
    }

    public Task OnDiceRolledAsync(Player player, int roll)
    {
        string name = PlayerName(player);
        if (roll == 0 && _rules.ZeroRollValue is { } effectiveZero)
            System.Console.WriteLine($"{name} rolled: {roll} (moves {effectiveZero})");
        else
            System.Console.WriteLine($"{name} rolled: {roll}");
        return Task.CompletedTask;
    }

    public Task OnMoveMadeAsync(Move move, MoveOutcome outcome)
    {
        string resultText = outcome.Result switch
        {
            MoveResult.Moved => "",
            MoveResult.ExtraTurn => " -> Extra turn!",
            MoveResult.Captured => " -> Captured!",
            MoveResult.CapturedAndExtraTurn => " -> Captured + Extra turn!",
            MoveResult.BorneOff => " -> Borne off!",
            MoveResult.BorneOffAndExtraTurn => " -> Borne off + Extra turn!",
            MoveResult.Win => " -> WIN!",
            _ => ""
        };
        System.Console.WriteLine($"  Piece #{move.PieceIndex} moved{resultText}");
        return Task.CompletedTask;
    }

    public Task OnTurnForfeitedAsync(Player player)
    {
        string name = PlayerName(player);
        System.Console.WriteLine($"  {name} has no valid moves — turn forfeited.");
        return Task.CompletedTask;
    }

    public Task OnGameOverAsync(Player winner)
    {
        string name = PlayerName(winner);
        System.Console.WriteLine();
        System.Console.WriteLine($"*** {name} wins the game! ***");
        return Task.CompletedTask;
    }

    private string PlayerName(Player player) =>
        player == Player.One ? _playerOne.Name : _playerTwo.Name;
}
