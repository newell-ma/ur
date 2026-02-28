using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public sealed class ConsolePlayer : ISkipCapablePlayer
{
    public string Name { get; }

    public ConsolePlayer(string name) => Name = name;

    public Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        if (validMoves.Count == 1)
        {
            System.Console.WriteLine($"  Only one move available: {FormatMove(validMoves[0], state.Rules)}");
            return Task.FromResult(validMoves[0]);
        }

        System.Console.WriteLine("  Available moves:");
        for (int i = 0; i < validMoves.Count; i++)
        {
            System.Console.WriteLine($"    [{i + 1}] {FormatMove(validMoves[i], state.Rules)}");
        }

        while (true)
        {
            System.Console.Write("  Choose move: ");
            string? input = System.Console.ReadLine();
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= validMoves.Count)
            {
                return Task.FromResult(validMoves[choice - 1]);
            }
            System.Console.WriteLine("  Invalid choice. Try again.");
        }
    }

    public Task<bool> ShouldSkipAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        System.Console.WriteLine("  Only backward moves available. Skip turn? (y/n)");
        while (true)
        {
            System.Console.Write("  > ");
            string? input = System.Console.ReadLine()?.Trim().ToLowerInvariant();
            switch (input)
            {
                case "y" or "yes": return Task.FromResult(true);
                case "n" or "no": return Task.FromResult(false);
            }
            System.Console.WriteLine("  Please enter y or n.");
        }
    }

    private static string FormatMove(Move move, GameRules rules)
    {
        string from = move.From == -1 ? "START" : $"pos {move.From}";
        string to = move.To == rules.PathLength ? "BEAR OFF" : $"pos {move.To}";
        string direction = move.To < move.From ? " (backward)" : "";
        return $"Piece #{move.PieceIndex}: {from} -> {to}{direction}";
    }
}
