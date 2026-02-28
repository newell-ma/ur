using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public static class BoardRenderer
{
    /// <summary>
    /// Renders the Royal Game of Ur board in a 3-row layout.
    ///
    /// The Finkel board layout (path length 15, shared lane 5-12):
    ///
    ///   P1 private:  0  1  2  3  [4]         [14] 13
    ///   Shared:                    5  6  7 [8] 9 10 11 12
    ///   P2 private:  0  1  2  3  [4]         [14] 13
    ///
    /// Rosettes shown with * marker.
    /// P1 pieces shown as 1, P2 pieces shown as 2.
    /// </summary>
    public static void Render(GameState state)
    {
        var rules = state.Rules;
        System.Console.WriteLine();
        System.Console.WriteLine(RenderTopRow(state, Player.One, rules));
        System.Console.WriteLine(RenderMiddleRow(state, rules));
        System.Console.WriteLine(RenderTopRow(state, Player.Two, rules));
        System.Console.WriteLine();
        System.Console.WriteLine($"  P1 ({PlayerSymbol(Player.One)}): {state.PiecesAtStart(Player.One)} at start, {state.PiecesBorneOff(Player.One)} borne off");
        System.Console.WriteLine($"  P2 ({PlayerSymbol(Player.Two)}): {state.PiecesAtStart(Player.Two)} at start, {state.PiecesBorneOff(Player.Two)} borne off");
        System.Console.WriteLine();
    }

    private static string RenderTopRow(GameState state, Player player, GameRules rules)
    {
        // Private lane: positions 0-4 then gap then 13-14
        // For Finkel: 0 1 2 3 4 _ _ _ _ _ _ _ _ 13 14
        // But only 0-4 and 13-14 are private (not in shared lane)
        var cells = new string[8]; // 0,1,2,3,4, gap, gap, gap = positions 0-4 then 14,13

        for (int i = 0; i <= 3; i++)
            cells[i] = CellContent(state, player, i, rules);

        cells[4] = CellContent(state, player, 4, rules);

        // Gap for shared lane
        cells[5] = "      ";
        cells[6] = "      ";

        // Reverse exit: 14 then 13 to match board shape
        // Actually the standard layout shows the exit path going back
        // positions 13 and 14 are the last two private squares
        cells[7] = CellContent(state, player, 14, rules) + " " + CellContent(state, player, 13, rules);

        char symbol = PlayerSymbol(player);
        return $"  {symbol}: {string.Join(" ", cells[0], cells[1], cells[2], cells[3], cells[4])}{cells[5]}{cells[7]}";
    }

    private static string RenderMiddleRow(GameState state, GameRules rules)
    {
        // Shared lane: positions 5-12
        var cells = new List<string>();
        for (int i = 5; i <= 12; i++)
        {
            cells.Add(SharedCellContent(state, i, rules));
        }
        return $"     {new string(' ', 15)}{string.Join(" ", cells)}";
    }

    private static string CellContent(GameState state, Player player, int position, GameRules rules)
    {
        bool isRosette = rules.IsRosette(position);
        bool occupied = state.IsOccupiedBy(player, position);
        char marker = isRosette ? '*' : '.';

        if (occupied)
            return $"[{PlayerSymbol(player)}]";
        return $"[{marker}]";
    }

    private static string SharedCellContent(GameState state, int position, GameRules rules)
    {
        bool isRosette = rules.IsRosette(position);
        char marker = isRosette ? '*' : '.';

        bool p1 = state.IsOccupiedBy(Player.One, position);
        bool p2 = state.IsOccupiedBy(Player.Two, position);

        if (p1) return $"[{PlayerSymbol(Player.One)}]";
        if (p2) return $"[{PlayerSymbol(Player.Two)}]";
        return $"[{marker}]";
    }

    private static char PlayerSymbol(Player player) => player == Player.One ? '1' : '2';
}
