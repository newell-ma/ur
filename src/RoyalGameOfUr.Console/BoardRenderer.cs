using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Console;

public static class BoardRenderer
{
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

        if (rules.PathLength == 16)
            RenderMastersBoard(state, rules);
        else
            RenderBellBoard(state, rules);

        System.Console.WriteLine();
        System.Console.WriteLine($"  P1 ({PlayerSymbol(Player.One)}): {state.PiecesAtStart(Player.One)} at start, {state.PiecesBorneOff(Player.One)} borne off");
        System.Console.WriteLine($"  P2 ({PlayerSymbol(Player.Two)}): {state.PiecesAtStart(Player.Two)} at start, {state.PiecesBorneOff(Player.Two)} borne off");
        System.Console.WriteLine();

        if (_interactive)
        {
            int width = System.Console.BufferWidth;
            string blank = new string(' ', width);
            for (int i = 0; i < MessageAreaLines; i++)
                System.Console.WriteLine(blank);

            System.Console.SetCursorPosition(0, _boardTop + BoardLines);
        }
    }

    #region Bell Path Rendering

    private static void RenderBellBoard(GameState state, GameRules rules)
    {
        System.Console.WriteLine(RenderBellTopRow(state, Player.One, rules));
        System.Console.WriteLine(RenderBellMiddleRow(state, rules));
        System.Console.WriteLine(RenderBellTopRow(state, Player.Two, rules));
    }

    private static string RenderBellTopRow(GameState state, Player player, GameRules rules)
    {
        var entry = new string[4];
        for (int i = 0; i < 4; i++)
            entry[i] = CellContent(state, player, 3 - i, rules);

        string exit = CellContent(state, player, 14, rules) + " " + CellContent(state, player, 13, rules);

        string gap = new string(' ', 13);
        char symbol = PlayerSymbol(player);
        return $"  {symbol}: {string.Join(" ", entry)}{gap}{exit}";
    }

    private static string RenderBellMiddleRow(GameState state, GameRules rules)
    {
        var cells = new List<string>();
        for (int i = 4; i <= 12; i++)
        {
            cells.Add(SharedCellContent(state, i, i, rules));
        }
        return $"     {string.Join(" ", cells)}";
    }

    #endregion

    #region Masters Path Rendering

    private static void RenderMastersBoard(GameState state, GameRules rules)
    {
        System.Console.WriteLine(RenderMastersPlayerRow(state, Player.One, rules));
        System.Console.WriteLine(RenderMastersMiddleRow(state, rules));
        System.Console.WriteLine(RenderMastersPlayerRow(state, Player.Two, rules));
    }

    private static string RenderMastersPlayerRow(GameState state, Player player, GameRules rules)
    {
        // Private lane: positions 0-3 (right to left: [3] [2] [1] [0])
        var entry = new string[4];
        for (int i = 0; i < 4; i++)
            entry[i] = CellContent(state, player, 3 - i, rules);

        // Exit zone: columns 7 and 8 of the physical board
        // Col 7: player's pos 15 and opponent's pos 11 share the physical tile
        // Col 8: player's pos 14 and opponent's pos 12 share the physical tile
        string col7 = CrossZoneCellContent(state, player, 15, 11, rules);
        string col8 = CrossZoneCellContent(state, player, 14, 12, rules);

        // Gap: 4 entry cells = 15 chars, 2 exit cells = 7 chars
        // Middle has 8 cells = 31 chars, total = 31
        // gap = 31 - 15 - 7 = 9
        string gap = new string(' ', 9);
        char symbol = PlayerSymbol(player);
        return $"  {symbol}: {string.Join(" ", entry)}{gap}{col7} {col8}";
    }

    private static string RenderMastersMiddleRow(GameState state, GameRules rules)
    {
        // Positions 4-10 (shared middle) + position 13 (shared, tile (2,8))
        var cells = new List<string>();
        for (int i = 4; i <= 10; i++)
        {
            cells.Add(SharedCellContent(state, i, i, rules));
        }
        cells.Add(SharedCellContent(state, 13, 13, rules));
        return $"     {string.Join(" ", cells)}";
    }

    /// <summary>
    /// Renders a cross-zone cell where one player's position and the opponent's
    /// position share the same physical tile.
    /// </summary>
    private static string CrossZoneCellContent(GameState state, Player player,
        int playerPos, int opponentPos, GameRules rules)
    {
        bool isRosette = rules.IsRosette(playerPos);
        char marker = isRosette ? '*' : '.';

        bool hasPlayerPiece = state.IsOccupiedBy(player, playerPos);
        bool hasOpponentPiece = state.IsOccupiedBy(player.Opponent(), opponentPos);

        return (hasPlayerPiece, hasOpponentPiece) switch
        {
            (true, true) => "[!]",
            (true, false) => $"[{PlayerSymbol(player)}]",
            (false, true) => $"[{PlayerSymbol(player.Opponent())}]",
            (false, false) => $"[{marker}]",
        };
    }

    #endregion

    #region Shared Cell Helpers

    private static string CellContent(GameState state, Player player, int position, GameRules rules)
    {
        bool isRosette = rules.IsRosette(position);
        bool occupied = state.IsOccupiedBy(player, position);
        char marker = isRosette ? '*' : '.';

        if (occupied)
        {
            int count = state.PieceCountAt(player, position);
            if (count > 1)
                return $"[{count}]"; // show stack count
            return $"[{PlayerSymbol(player)}]";
        }
        return $"[{marker}]";
    }

    private static string SharedCellContent(GameState state, int p1Pos, int p2Pos, GameRules rules)
    {
        bool isRosette = rules.IsRosette(p1Pos);
        char marker = isRosette ? '*' : '.';

        bool p1 = state.IsOccupiedBy(Player.One, p1Pos);
        bool p2 = state.IsOccupiedBy(Player.Two, p2Pos);
        int p1Count = p1 ? state.PieceCountAt(Player.One, p1Pos) : 0;
        int p2Count = p2 ? state.PieceCountAt(Player.Two, p2Pos) : 0;

        return (p1, p1Count, p2, p2Count) switch
        {
            (true, > 1, _, _) => $"[{p1Count}]",
            (true, _, _, _) => $"[{PlayerSymbol(Player.One)}]",
            (_, _, true, > 1) => $"[{p2Count}]",
            (_, _, true, _) => $"[{PlayerSymbol(Player.Two)}]",
            _ => $"[{marker}]",
        };
    }

    #endregion

    #region Index Rendering

    public static void RenderIndexes(GameRules rules)
    {
        if (rules.PathLength == 16)
            RenderMastersIndexes(rules);
        else
            RenderBellIndexes(rules);
    }

    private static void RenderBellIndexes(GameRules rules)
    {
        static string Idx(int pos, GameRules r) =>
            r.IsRosette(pos) ? $"[{pos,2}*]" : $"[{pos,2} ]";

        string entry = string.Join("", new[] { 3, 2, 1, 0 }.Select(i => Idx(i, rules)));
        string exit = Idx(14, rules) + Idx(13, rules);
        int middleWidth = 9 * 5;
        int gap = middleWidth - entry.Length - exit.Length;

        string privateRow = $"     {entry}{new string(' ', gap)}{exit}";
        string middle = string.Join("", Enumerable.Range(4, 9).Select(i => Idx(i, rules)));

        System.Console.WriteLine();
        System.Console.WriteLine($"  1:{privateRow[4..]}");
        System.Console.WriteLine($"     {middle}");
        System.Console.WriteLine($"  2:{privateRow[4..]}");
        System.Console.WriteLine();
    }

    private static void RenderMastersIndexes(GameRules rules)
    {
        static string Idx(int pos, GameRules r) =>
            r.IsRosette(pos) ? $"[{pos,2}*]" : $"[{pos,2} ]";

        string entry = string.Join("", new[] { 3, 2, 1, 0 }.Select(i => Idx(i, rules)));
        string exit = Idx(15, rules) + Idx(14, rules);
        // Middle: 8 cells (4-10 + 13)
        int middleWidth = 8 * 5;
        int gap = middleWidth - entry.Length - exit.Length;
        if (gap < 1) gap = 1;

        string privateRow = $"     {entry}{new string(' ', gap)}{exit}";
        string middle = string.Join("",
            Enumerable.Range(4, 7).Select(i => Idx(i, rules))
                .Append(Idx(13, rules)));

        System.Console.WriteLine();
        System.Console.WriteLine($"  1:{privateRow[4..]}");
        System.Console.WriteLine($"     {middle}");
        System.Console.WriteLine($"  2:{privateRow[4..]}");
        System.Console.WriteLine();
        System.Console.WriteLine("  Cross-zone: P1 pos 11/12 on P2's row, P2 pos 11/12 on P1's row");
    }

    #endregion

    private static char PlayerSymbol(Player player) => player == Player.One ? '1' : '2';
}
