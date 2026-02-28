using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Console;

bool demo = args.Contains("--demo");
bool showIndexes = args.Contains("--indexes");

Console.WriteLine("=== The Royal Game of Ur ===");
Console.WriteLine();

if (showIndexes)
{
    BoardRenderer.RenderIndexes(GameRules.Finkel);
    return;
}

IPlayer player1;
IPlayer player2;

if (demo)
{
    Console.WriteLine("Demo mode: AI vs AI");
    Console.WriteLine();
    player1 = new GreedyAiPlayer("AI 1");
    player2 = new GreedyAiPlayer("AI 2");
}
else
{
    Console.Write("Player 1 name (default: Player 1): ");
    string? p1Name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(p1Name)) p1Name = "Player 1";

    Console.Write("Player 2 name (default: Player 2): ");
    string? p2Name = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(p2Name)) p2Name = "Player 2";

    Console.WriteLine();

    player1 = new ConsolePlayer(p1Name);
    player2 = new ConsolePlayer(p2Name);
}

var rules = GameRules.Finkel;
var dice = new Dice();
var game = new Game(dice, rules);

var runner = new GameRunner(game, player1, player2);

runner.OnStateChanged += state =>
{
    BoardRenderer.Render(state);
};

runner.OnDiceRolled += (player, roll) =>
{
    string name = player == Player.One ? player1.Name : player2.Name;
    Console.WriteLine($"{name} rolled: {roll}");
};

runner.OnMoveMade += (move, result) =>
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
    Console.WriteLine($"  Move executed{resultText}");
};

runner.OnTurnForfeited += player =>
{
    string name = player == Player.One ? player1.Name : player2.Name;
    Console.WriteLine($"  {name} has no valid moves — turn forfeited.");
};

runner.OnGameOver += winner =>
{
    string name = winner == Player.One ? player1.Name : player2.Name;
    Console.WriteLine();
    Console.WriteLine($"*** {name} wins the game! ***");
};

await runner.RunAsync();
