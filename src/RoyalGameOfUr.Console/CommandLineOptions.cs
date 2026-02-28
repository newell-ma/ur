using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public sealed class CommandLineOptions
{
    public GameRules? Rules { get; private init; }
    public bool IsDemo { get; private init; }
    public bool ShowIndexes { get; private init; }

    public static CommandLineOptions Parse(string[] args)
    {
        bool demo = args.Contains("--demo");
        bool showIndexes = args.Contains("--indexes");

        GameRules? rules = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--rules=", StringComparison.OrdinalIgnoreCase))
            {
                string name = arg["--rules=".Length..].ToLowerInvariant();
                rules = name switch
                {
                    "finkel" => GameRules.Finkel,
                    "simple" => GameRules.Simple,
                    "masters" => GameRules.Masters,
                    "blitz" => GameRules.Blitz,
                    "tournament" => GameRules.Tournament,
                    _ => throw new ArgumentException(
                        $"Unknown ruleset: '{name}'. Valid options: finkel, simple, masters, blitz, tournament")
                };
            }
        }

        return new CommandLineOptions
        {
            Rules = rules,
            IsDemo = demo,
            ShowIndexes = showIndexes
        };
    }
}
