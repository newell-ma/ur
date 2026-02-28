using RoyalGameOfUr.Engine;
using Spectre.Console;

namespace RoyalGameOfUr.Console;

public sealed class SpectreGameSetup : IGameSetup
{
    private readonly CommandLineOptions _options;

    public SpectreGameSetup(CommandLineOptions options) => _options = options;

    public Task<GameConfiguration> ConfigureAsync(CancellationToken ct = default)
    {
        AnsiConsole.MarkupLine("[bold yellow]=== The Royal Game of Ur ===[/]");
        AnsiConsole.WriteLine();

        var rules = _options.Rules ?? PromptForRules();

        AnsiConsole.MarkupLine($"[dim]Ruleset:[/] [bold]{rules.Name}[/]");
        AnsiConsole.WriteLine();

        PlayerConfiguration player1;
        PlayerConfiguration player2;

        if (_options.IsDemo)
        {
            player1 = new PlayerConfiguration("AI 1", PlayerType.Computer);
            player2 = new PlayerConfiguration("AI 2", PlayerType.Computer);
            AnsiConsole.MarkupLine("[dim]Demo mode: AI vs AI[/]");
        }
        else
        {
            player1 = PromptForPlayer("Player 1");
            player2 = PromptForPlayer("Player 2");
        }

        AnsiConsole.WriteLine();

        return Task.FromResult(new GameConfiguration(rules, player1, player2));
    }

    private static GameRules PromptForRules()
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose a [bold]ruleset[/]:")
                .AddChoices(
                    "Finkel (classic, 7 pieces, 4 dice)",
                    "Simple (2 rosettes, 7 pieces, 4 dice)",
                    "Masters (3 dice, zero=4, cross-captures)",
                    "Blitz (5 pieces, capture extra roll)",
                    "Tournament (stacking, backward moves, voluntary skip)"));

        return choice.Split(' ')[0] switch
        {
            "Finkel" => GameRules.Finkel,
            "Simple" => GameRules.Simple,
            "Masters" => GameRules.Masters,
            "Blitz" => GameRules.Blitz,
            "Tournament" => GameRules.Tournament,
            _ => GameRules.Finkel
        };
    }

    private static PlayerConfiguration PromptForPlayer(string label)
    {
        var type = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{label}[/] type:")
                .AddChoices("Human", "Computer (AI)"));

        var playerType = type.StartsWith("Computer") ? PlayerType.Computer : PlayerType.Human;

        string defaultName = playerType == PlayerType.Computer ? $"AI ({label})" : label;

        var name = AnsiConsole.Prompt(
            new TextPrompt<string>($"[bold]{label}[/] name:")
                .DefaultValue(defaultName)
                .AllowEmpty());

        if (string.IsNullOrWhiteSpace(name))
            name = defaultName;

        return new PlayerConfiguration(name, playerType);
    }
}
