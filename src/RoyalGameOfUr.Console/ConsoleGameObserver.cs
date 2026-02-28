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

    public void OnStateChanged(GameState state)
    {
        BoardRenderer.Render(state);
    }

    public void OnDiceRolled(Player player, int roll)
    {
        string name = PlayerName(player);
        if (_rules.ZeroRollValue.HasValue && roll == 0)
            System.Console.WriteLine($"{name} rolled: {roll} (moves {_rules.ZeroRollValue.Value})");
        else
            System.Console.WriteLine($"{name} rolled: {roll}");
    }

    public void OnMoveMade(Move move, MoveResult result)
    {
        string resultText = result switch
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
        System.Console.WriteLine($"  Move executed{resultText}");
    }

    public void OnTurnForfeited(Player player)
    {
        string name = PlayerName(player);
        System.Console.WriteLine($"  {name} has no valid moves — turn forfeited.");
    }

    public void OnGameOver(Player winner)
    {
        string name = PlayerName(winner);
        System.Console.WriteLine();
        System.Console.WriteLine($"*** {name} wins the game! ***");
    }

    private string PlayerName(Player player) =>
        player == Player.One ? _playerOne.Name : _playerTwo.Name;
}
