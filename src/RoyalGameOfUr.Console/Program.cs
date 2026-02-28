using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Console;

bool demo = args.Contains("--demo");
bool showIndexes = args.Contains("--indexes");

// Parse --rules=<name> flag
string rulesName = "finkel";
foreach (var arg in args)
{
    if (arg.StartsWith("--rules=", StringComparison.OrdinalIgnoreCase))
    {
        rulesName = arg["--rules=".Length..].ToLowerInvariant();
    }
}

GameRules rules = rulesName switch
{
    "finkel" => GameRules.Finkel,
    "simple" => GameRules.Simple,
    "masters" => GameRules.Masters,
    "blitz" => GameRules.Blitz,
    "tournament" => GameRules.Tournament,
    _ => throw new ArgumentException(
        $"Unknown ruleset: '{rulesName}'. Valid options: finkel, simple, masters, blitz, tournament")
};

Console.WriteLine("=== The Royal Game of Ur ===");
Console.WriteLine($"  Ruleset: {rules.Name}");
Console.WriteLine();

if (showIndexes)
{
    BoardRenderer.RenderIndexes(rules);
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

var dice = new Dice(null, rules.DiceCount);
var game = new Game(dice, rules);
var observer = new ConsoleGameObserver(player1, player2, rules);
var runner = new GameRunner(game, player1, player2, observer);

await runner.RunAsync();
