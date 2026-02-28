using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Models;

namespace RoyalGameOfUr.Web.Services;

public sealed class BoardCoordinateMapper
{
    public const double CellSize = 76;
    public const double CellGap = 6;
    public const double Padding = 20;

    private readonly GameRules _rules;
    private readonly List<BoardCell> _cells = new();
    private readonly Dictionary<(Player Player, int Position), (double X, double Y)> _positionMap = new();

    public IReadOnlyList<BoardCell> Cells => _cells;
    public int Columns { get; private set; }
    public int Rows => 3;
    public double SvgWidth => Padding * 2 + Columns * (CellSize + CellGap) - CellGap;
    public double SvgHeight => Padding * 2 + Rows * (CellSize + CellGap) - CellGap;

    public BoardCoordinateMapper(GameRules rules)
    {
        _rules = rules;
        if (rules.PathLength == 15)
            BuildBellPath(rules);
        else
            BuildMastersPath(rules);
    }

    public (double X, double Y) GetCellTopLeft(int col, int row)
    {
        double x = Padding + col * (CellSize + CellGap);
        double y = Padding + row * (CellSize + CellGap);
        return (x, y);
    }

    public (double X, double Y) GetPieceCenter(Player player, int position)
    {
        if (_positionMap.TryGetValue((player, position), out var coords))
            return coords;
        return (0, 0);
    }

    public (double X, double Y) GetPieceCenterWithOffset(Player player, int position, int stackIndex)
    {
        var (cx, cy) = GetPieceCenter(player, position);
        if (stackIndex > 0)
        {
            cx += stackIndex * 6;
            cy -= stackIndex * 6;
        }
        return (cx, cy);
    }

    private void RegisterPosition(Player player, int position, int col, int row)
    {
        var (x, y) = GetCellTopLeft(col, row);
        _positionMap[(player, position)] = (x + CellSize / 2, y + CellSize / 2);
    }

    private void BuildBellPath(GameRules rules)
    {
        Columns = 9;

        // Bell path layout:
        // Col:   0    1    2    3    4    5    6    7    8
        // Row 0: [3]  [2]  [1]  [0]  ---  ---  ---  [14] [13]   P1 private + exit
        // Row 1: [4]  [5]  [6]  [7]  [8]  [9]  [10] [11] [12]   Shared
        // Row 2: [3]  [2]  [1]  [0]  ---  ---  ---  [14] [13]   P2 private + exit

        // P1 private lane (row 0, cols 0-3) — positions 3,2,1,0
        for (int col = 0; col < 4; col++)
        {
            int pos = 3 - col;
            var cell = new BoardCell
            {
                Col = col, Row = 0,
                IsRosette = rules.IsRosette(pos),
                Zone = "private1",
                Positions = { [Player.One] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.One, pos, col, 0);
        }

        // P1 exit (row 0, cols 7-8) — positions 14,13
        for (int col = 7; col <= 8; col++)
        {
            int pos = 7 + 14 - col; // col 7 -> 14, col 8 -> 13
            var cell = new BoardCell
            {
                Col = col, Row = 0,
                IsRosette = rules.IsRosette(pos),
                Zone = "exit1",
                Positions = { [Player.One] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.One, pos, col, 0);
        }

        // Shared lane (row 1, cols 0-8) — positions 4-12
        for (int col = 0; col < 9; col++)
        {
            int pos = col + 4;
            var cell = new BoardCell
            {
                Col = col, Row = 1,
                IsRosette = rules.IsRosette(pos),
                Zone = "shared",
                Positions = { [Player.One] = pos, [Player.Two] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.One, pos, col, 1);
            RegisterPosition(Player.Two, pos, col, 1);
        }

        // P2 private lane (row 2, cols 0-3) — positions 3,2,1,0
        for (int col = 0; col < 4; col++)
        {
            int pos = 3 - col;
            var cell = new BoardCell
            {
                Col = col, Row = 2,
                IsRosette = rules.IsRosette(pos),
                Zone = "private2",
                Positions = { [Player.Two] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.Two, pos, col, 2);
        }

        // P2 exit (row 2, cols 7-8) — positions 14,13
        for (int col = 7; col <= 8; col++)
        {
            int pos = 7 + 14 - col;
            var cell = new BoardCell
            {
                Col = col, Row = 2,
                IsRosette = rules.IsRosette(pos),
                Zone = "exit2",
                Positions = { [Player.Two] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.Two, pos, col, 2);
        }
    }

    private void BuildMastersPath(GameRules rules)
    {
        Columns = 9;

        // Masters path layout:
        // Col:   0    1    2    3    4    5    6    7       8
        // Row 0: [3]  [2]  [1]  [0]  ---  ---  ---  [15/11] [14/12]  P1 priv + cross
        // Row 1: [4]  [5]  [6]  [7]  [8]  [9]  [10] ---     [13]     Shared
        // Row 2: [3]  [2]  [1]  [0]  ---  ---  ---  [15/11] [14/12]  P2 priv + cross

        // P1 private lane (row 0, cols 0-3)
        for (int col = 0; col < 4; col++)
        {
            int pos = 3 - col;
            var cell = new BoardCell
            {
                Col = col, Row = 0,
                IsRosette = rules.IsRosette(pos),
                Zone = "private1",
                Positions = { [Player.One] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.One, pos, col, 0);
        }

        // Cross-zone cells for P1 (row 0, cols 7-8)
        // Col 7: P1 pos 15, P2 pos 11 (cross-capture zone)
        _cells.Add(new BoardCell
        {
            Col = 7, Row = 0,
            IsRosette = rules.IsRosette(15) || rules.IsRosette(11),
            Zone = "cross1",
            Positions = { [Player.One] = 15, [Player.Two] = 11 }
        });
        RegisterPosition(Player.One, 15, 7, 0);
        RegisterPosition(Player.Two, 11, 7, 0);

        // Col 8: P1 pos 14, P2 pos 12
        _cells.Add(new BoardCell
        {
            Col = 8, Row = 0,
            IsRosette = rules.IsRosette(14) || rules.IsRosette(12),
            Zone = "cross1",
            Positions = { [Player.One] = 14, [Player.Two] = 12 }
        });
        RegisterPosition(Player.One, 14, 8, 0);
        RegisterPosition(Player.Two, 12, 8, 0);

        // Shared lane (row 1, cols 0-6) — positions 4-10
        for (int col = 0; col <= 6; col++)
        {
            int pos = col + 4;
            var cell = new BoardCell
            {
                Col = col, Row = 1,
                IsRosette = rules.IsRosette(pos),
                Zone = "shared",
                Positions = { [Player.One] = pos, [Player.Two] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.One, pos, col, 1);
            RegisterPosition(Player.Two, pos, col, 1);
        }

        // Shared position 13 (row 1, col 8)
        _cells.Add(new BoardCell
        {
            Col = 8, Row = 1,
            IsRosette = rules.IsRosette(13),
            Zone = "shared",
            Positions = { [Player.One] = 13, [Player.Two] = 13 }
        });
        RegisterPosition(Player.One, 13, 8, 1);
        RegisterPosition(Player.Two, 13, 8, 1);

        // P2 private lane (row 2, cols 0-3)
        for (int col = 0; col < 4; col++)
        {
            int pos = 3 - col;
            var cell = new BoardCell
            {
                Col = col, Row = 2,
                IsRosette = rules.IsRosette(pos),
                Zone = "private2",
                Positions = { [Player.Two] = pos }
            };
            _cells.Add(cell);
            RegisterPosition(Player.Two, pos, col, 2);
        }

        // Cross-zone cells for P2 (row 2, cols 7-8)
        // Col 7: P2 pos 15, P1 pos 11
        _cells.Add(new BoardCell
        {
            Col = 7, Row = 2,
            IsRosette = rules.IsRosette(15) || rules.IsRosette(11),
            Zone = "cross2",
            Positions = { [Player.Two] = 15, [Player.One] = 11 }
        });
        RegisterPosition(Player.Two, 15, 7, 2);
        RegisterPosition(Player.One, 11, 7, 2);

        // Col 8: P2 pos 14, P1 pos 12
        _cells.Add(new BoardCell
        {
            Col = 8, Row = 2,
            IsRosette = rules.IsRosette(14) || rules.IsRosette(12),
            Zone = "cross2",
            Positions = { [Player.Two] = 14, [Player.One] = 12 }
        });
        RegisterPosition(Player.Two, 14, 8, 2);
        RegisterPosition(Player.One, 12, 8, 2);
    }
}
