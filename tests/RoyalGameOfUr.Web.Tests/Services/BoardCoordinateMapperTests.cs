using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class BoardCoordinateMapperTests
{
    [Test]
    public async Task BellPath_Has9Columns()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);
        await Assert.That(mapper.Columns).IsEqualTo(9);
    }

    [Test]
    public async Task MastersPath_Has9Columns()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);
        await Assert.That(mapper.Columns).IsEqualTo(9);
    }

    [Test]
    public async Task BellPath_CellCount()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);
        await Assert.That(mapper.Cells.Count).IsEqualTo(21);
    }

    [Test]
    public async Task MastersPath_CellCount()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);
        await Assert.That(mapper.Cells.Count).IsEqualTo(20);
    }

    [Test]
    public async Task SvgDimensions_Computed()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);

        double expectedWidth = BoardCoordinateMapper.Padding * 2
            + mapper.Columns * (BoardCoordinateMapper.CellSize + BoardCoordinateMapper.CellGap)
            - BoardCoordinateMapper.CellGap;
        double expectedHeight = BoardCoordinateMapper.Padding * 2
            + mapper.Rows * (BoardCoordinateMapper.CellSize + BoardCoordinateMapper.CellGap)
            - BoardCoordinateMapper.CellGap;

        await Assert.That(mapper.SvgWidth).IsEqualTo(expectedWidth);
        await Assert.That(mapper.SvgHeight).IsEqualTo(expectedHeight);
    }

    [Test]
    [Arguments(Player.One)]
    [Arguments(Player.Two)]
    public async Task GetPieceCenter_AllPositions_Valid(Player player)
    {
        var rules = GameRules.Finkel;
        var mapper = new BoardCoordinateMapper(rules);

        for (int pos = 0; pos < rules.PathLength; pos++)
        {
            var (x, y) = mapper.GetPieceCenter(player, pos);
            await Assert.That(x).IsGreaterThan(0);
            await Assert.That(y).IsGreaterThan(0);
        }
    }

    [Test]
    [Arguments(Player.One)]
    [Arguments(Player.Two)]
    public async Task GetPieceCenter_MastersPath_AllPositions_Valid(Player player)
    {
        var rules = GameRules.Masters;
        var mapper = new BoardCoordinateMapper(rules);

        for (int pos = 0; pos < rules.PathLength; pos++)
        {
            var (x, y) = mapper.GetPieceCenter(player, pos);
            await Assert.That(x).IsGreaterThan(0);
            await Assert.That(y).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task RosetteCells_Marked()
    {
        var rules = GameRules.Finkel;
        var mapper = new BoardCoordinateMapper(rules);

        var rosetteCells = mapper.Cells.Where(c => c.IsRosette).ToList();
        await Assert.That(rosetteCells).IsNotEmpty();

        foreach (var cell in rosetteCells)
        {
            var anyRosette = cell.Positions.Values.Any(pos => rules.IsRosette(pos));
            await Assert.That(anyRosette).IsTrue();
        }
    }

    [Test]
    public async Task CrossZoneCells_BothPlayers()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Masters);

        var crossCells = mapper.Cells.Where(c => c.Zone.StartsWith("cross")).ToList();
        await Assert.That(crossCells).IsNotEmpty();

        foreach (var cell in crossCells)
        {
            await Assert.That(cell.Positions.ContainsKey(Player.One)).IsTrue();
            await Assert.That(cell.Positions.ContainsKey(Player.Two)).IsTrue();
        }
    }

    [Test]
    public async Task GetPieceCenterWithOffset_AppliesStackOffset()
    {
        var mapper = new BoardCoordinateMapper(GameRules.Finkel);

        var (cx0, cy0) = mapper.GetPieceCenterWithOffset(Player.One, 4, 0);
        var (cx1, cy1) = mapper.GetPieceCenterWithOffset(Player.One, 4, 1);

        await Assert.That(cx1).IsEqualTo(cx0 + 6);
        await Assert.That(cy1).IsEqualTo(cy0 - 6);
    }

    [Test]
    public async Task Rows_Always3()
    {
        await Assert.That(new BoardCoordinateMapper(GameRules.Finkel).Rows).IsEqualTo(3);
        await Assert.That(new BoardCoordinateMapper(GameRules.Masters).Rows).IsEqualTo(3);
    }
}
