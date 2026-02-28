using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Console;

var options = CommandLineOptions.Parse(args);

if (options.ShowIndexes)
{
    var rules = options.Rules ?? GameRules.Finkel;
    BoardRenderer.RenderIndexes(rules);
    return;
}

var config = await new SpectreGameSetup(options).ConfigureAsync();

var player1 = CreatePlayer(config.Player1);
var player2 = CreatePlayer(config.Player2);

var dice = new Dice(null, config.Rules.DiceCount);
var game = new Game(dice, config.Rules);
var observer = new ConsoleGameObserver(player1, player2, config.Rules);
var runner = new GameRunner(game, player1, player2, observer);

await runner.RunAsync();

static IPlayer CreatePlayer(PlayerConfiguration playerConfig) =>
    playerConfig.Type == PlayerType.Computer
        ? new GreedyAiPlayer(playerConfig.Name)
        : new ConsolePlayer(playerConfig.Name);
