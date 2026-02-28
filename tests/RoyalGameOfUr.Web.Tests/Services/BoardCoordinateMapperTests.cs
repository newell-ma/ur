using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class BoardCoordinateMapperTests
{
    [Fact]
    public void BellPath_Has9Columns()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);
        Assert.Equal(9, mapper.Columns);
    }

    [Fact]
    public void MastersPath_Has9Columns()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);
        Assert.Equal(9, mapper.Columns);
    }

    [Fact]
    public void BellPath_CellCount()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);
        // P1 private: 4, P1 exit: 2, shared: 9, P2 private: 4, P2 exit: 2 = 21
        // Wait — let me count from the code:
        // P1 private (4) + P1 exit (2) + shared (9) + P2 private (4) + P2 exit (2) = 21
        Assert.Equal(21, mapper.Cells.Count);
    }

    [Fact]
    public void MastersPath_CellCount()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);
        // P1 private (4) + cross1 (2) + shared (7 + 1 for pos 13) + P2 private (4) + cross2 (2) = 20
        Assert.Equal(20, mapper.Cells.Count);
    }

    [Fact]
    public void SvgDimensions_Computed()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);

        double expectedWidth = BoardCoordinateMapper.Padding * 2
            + mapper.Columns * (BoardCoordinateMapper.CellSize + BoardCoordinateMapper.CellGap)
            - BoardCoordinateMapper.CellGap;
        double expectedHeight = BoardCoordinateMapper.Padding * 2
            + mapper.Rows * (BoardCoordinateMapper.CellSize + BoardCoordinateMapper.CellGap)
            - BoardCoordinateMapper.CellGap;

        Assert.Equal(expectedWidth, mapper.SvgWidth);
        Assert.Equal(expectedHeight, mapper.SvgHeight);
    }

    [Theory]
    [InlineData(Player.One)]
    [InlineData(Player.Two)]
    public void GetPieceCenter_AllPositions_Valid(Player player)
    {
        var rules = GameRules.Finkel;
        var mapper = new BoardCoordinateMapper(rules);

        for (int pos = 0; pos < rules.PathLength; pos++)
        {
            var (x, y) = mapper.GetPieceCenter(player, pos);
            Assert.True(x > 0, $"Player {player} position {pos}: X should be positive, got {x}");
            Assert.True(y > 0, $"Player {player} position {pos}: Y should be positive, got {y}");
        }
    }

    [Theory]
    [InlineData(Player.One)]
    [InlineData(Player.Two)]
    public void GetPieceCenter_MastersPath_AllPositions_Valid(Player player)
    {
        var rules = GameRules.Masters;
        var mapper = new BoardCoordinateMapper(rules);

        for (int pos = 0; pos < rules.PathLength; pos++)
        {
            var (x, y) = mapper.GetPieceCenter(player, pos);
            Assert.True(x > 0, $"Player {player} position {pos}: X should be positive, got {x}");
            Assert.True(y > 0, $"Player {player} position {pos}: Y should be positive, got {y}");
        }
    }

    [Fact]
    public void RosetteCells_Marked()
    {
        var rules = GameRules.Finkel;
        var mapper = new BoardCoordinateMapper(rules);

        var rosetteCells = mapper.Cells.Where(c => c.IsRosette).ToList();
        Assert.NotEmpty(rosetteCells);

        // Finkel rosettes at positions 4, 8, 14
        // Position 4 is in shared lane, position 8 is in shared lane, position 14 is in exit zones
        foreach (var cell in rosetteCells)
        {
            var anyRosette = cell.Positions.Values.Any(pos => rules.IsRosette(pos));
            Assert.True(anyRosette, $"Cell at ({cell.Col},{cell.Row}) marked as rosette but no position is a rosette");
        }
    }

    [Fact]
    public void CrossZoneCells_BothPlayers()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);

        var crossCells = mapper.Cells.Where(c => c.Zone.StartsWith("cross")).ToList();
        Assert.NotEmpty(crossCells);

        foreach (var cell in crossCells)
        {
            Assert.True(cell.Positions.ContainsKey(Player.One),
                $"Cross-zone cell ({cell.Col},{cell.Row}) missing Player.One position");
            Assert.True(cell.Positions.ContainsKey(Player.Two),
                $"Cross-zone cell ({cell.Col},{cell.Row}) missing Player.Two position");
        }
    }

    [Fact]
    public void GetPieceCenterWithOffset_AppliesStackOffset()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);

        var (cx0, cy0) = mapper.GetPieceCenterWithOffset(Player.One, 4, 0);
        var (cx1, cy1) = mapper.GetPieceCenterWithOffset(Player.One, 4, 1);

        // stackIndex 0 has no offset, stackIndex 1 has +6x, -6y
        Assert.Equal(cx0 + 6, cx1);
        Assert.Equal(cy0 - 6, cy1);
    }

    [Fact]
    public void Rows_Always3()
    {
        Assert.Equal(3, new BoardCoordinateMapper(GameRules.Finkel).Rows);
        Assert.Equal(3, new BoardCoordinateMapper(GameRules.Masters).Rows);
    }
}
