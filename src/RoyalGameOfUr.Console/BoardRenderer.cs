using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public static class BoardRenderer
{
    /// <summary>
    /// Renders the Royal Game of Ur board in a 3-row layout matching the
    /// physical H-shaped board (4-column left block, 2-column bridge, 2-column right block).
    ///
    /// The Finkel board layout (path length 15, shared lane 5-12):
    ///
    ///   P1:  [3] [2] [1] [0]                   [14*][13]
    ///        [4*][5]  [6] [7] [8*][9] [10] [11] [12]
    ///   P2:  [3] [2] [1] [0]                   [14*][13]
    ///
    /// Rosettes shown with * marker.
    /// P1 pieces shown as 1, P2 pieces shown as 2.
    /// </summary>
    private static int _boardTop = -1;
    private const int BoardLines = 8;
    private const int MessageAreaLines = 10;
    private static bool _interactive = true;

    public static void Render(GameState state)
    {
        var rules = state.Rules;

        if (_interactive)
        {
            try
            {
                if (_boardTop < 0)
                    _boardTop = System.Console.CursorTop;
                else
                    System.Console.SetCursorPosition(0, _boardTop);
            }
            catch (IOException)
            {
                _interactive = false;
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine(RenderTopRow(state, Player.One, rules));
        System.Console.WriteLine(RenderMiddleRow(state, rules));
        System.Console.WriteLine(RenderTopRow(state, Player.Two, rules));
        System.Console.WriteLine();
        System.Console.WriteLine($"  P1 ({PlayerSymbol(Player.One)}): {state.PiecesAtStart(Player.One)} at start, {state.PiecesBorneOff(Player.One)} borne off");
        System.Console.WriteLine($"  P2 ({PlayerSymbol(Player.Two)}): {state.PiecesAtStart(Player.Two)} at start, {state.PiecesBorneOff(Player.Two)} borne off");
        System.Console.WriteLine();

        if (_interactive)
        {
            // Clear stale messages from the previous turn
            int width = System.Console.BufferWidth;
            string blank = new string(' ', width);
            for (int i = 0; i < MessageAreaLines; i++)
                System.Console.WriteLine(blank);

            // Position cursor right after the board for new messages
            System.Console.SetCursorPosition(0, _boardTop + BoardLines);
        }
    }

    private static string RenderTopRow(GameState state, Player player, GameRules rules)
    {
        // Private lane: 4 cells (positions 0-3), matching the physical left block
        // Exit: 2 cells (positions 14, 13), right-aligned above last 2 middle cells
        var entry = new string[4];
        for (int i = 0; i < 4; i++)
            entry[i] = CellContent(state, player, 3 - i, rules);

        string exit = CellContent(state, player, 14, rules) + " " + CellContent(state, player, 13, rules);

        // Gap: total top row width must equal middle row width (9 cells = 35 chars)
        // 4 entry cells = 15 chars, 2 exit cells = 7 chars, gap = 35 - 15 - 7 = 13
        string gap = new string(' ', 13);

        char symbol = PlayerSymbol(player);
        return $"  {symbol}: {string.Join(" ", entry)}{gap}{exit}";
    }

    private static string RenderMiddleRow(GameState state, GameRules rules)
    {
        // Middle row: 9 cells (positions 4-12), left-aligned with private rows
        // Position 4 is private to each player but sits on the shared row physically
        var cells = new List<string>();
        for (int i = 4; i <= 12; i++)
        {
            cells.Add(SharedCellContent(state, i, rules));
        }
        return $"     {string.Join(" ", cells)}";
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

    public static void RenderIndexes(GameRules rules)
    {
        static string Idx(int pos, GameRules r) =>
            r.IsRosette(pos) ? $"[{pos,2}*]" : $"[{pos,2} ]";

        // Private row (positions 0-3) + gap + exit (14, 13)
        string entry = string.Join("", new[] { 3, 2, 1, 0 }.Select(i => Idx(i, rules)));
        string exit = Idx(14, rules) + Idx(13, rules);
        int middleWidth = 9 * 5; // 9 cells × 5 chars each
        int gap = middleWidth - entry.Length - exit.Length;

        string privateRow = $"     {entry}{new string(' ', gap)}{exit}";

        // Middle row (positions 4-12)
        string middle = string.Join("", Enumerable.Range(4, 9).Select(i => Idx(i, rules)));

        System.Console.WriteLine();
        System.Console.WriteLine($"  1:{privateRow[4..]}");
        System.Console.WriteLine($"     {middle}");
        System.Console.WriteLine($"  2:{privateRow[4..]}");
        System.Console.WriteLine();
    }

    private static char PlayerSymbol(Player player) => player == Player.One ? '1' : '2';
}
