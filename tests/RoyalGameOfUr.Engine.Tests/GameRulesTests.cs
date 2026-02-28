using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Engine.Tests;

public class GameRulesTests
{
    [Test]
    public async Task Finkel_HasCorrectRosettes()
    {
        var rules = GameRules.Finkel;

        await Assert.That(rules.RosettePositions).Contains(4);
        await Assert.That(rules.RosettePositions).Contains(8);
        await Assert.That(rules.RosettePositions).Contains(14);
        await Assert.That(rules.RosettePositions.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Finkel_Has7Pieces()
    {
        await Assert.That(GameRules.Finkel.PiecesPerPlayer).IsEqualTo(7);
    }

    [Test]
    public async Task Finkel_HasPathLength15()
    {
        await Assert.That(GameRules.Finkel.PathLength).IsEqualTo(15);
    }

    [Test]
    public async Task Finkel_SharedLane5To12()
    {
        var rules = GameRules.Finkel;
        await Assert.That(rules.SharedLaneStart).IsEqualTo(5);
        await Assert.That(rules.SharedLaneEnd).IsEqualTo(12);
    }

    [Test]
    public async Task Simple_HasTwoRosettes()
    {
        var rules = GameRules.Simple;

        await Assert.That(rules.RosettePositions).Contains(4);
        await Assert.That(rules.RosettePositions).Contains(8);
        await Assert.That(rules.RosettePositions.Count).IsEqualTo(2);
    }

    [Test]
    [Arguments(4, true)]
    [Arguments(8, true)]
    [Arguments(14, true)]
    [Arguments(1, false)]
    [Arguments(5, false)]
    [Arguments(12, false)]
    public async Task IsRosette_Finkel(int position, bool expected)
    {
        await Assert.That(GameRules.Finkel.IsRosette(position)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(4, false)]
    [Arguments(5, true)]
    [Arguments(8, true)]
    [Arguments(12, true)]
    [Arguments(13, false)]
    public async Task IsSharedLane_Finkel(int position, bool expected)
    {
        await Assert.That(GameRules.Finkel.IsSharedLane(position)).IsEqualTo(expected);
    }

    [Test]
    public async Task CustomRules_Work()
    {
        var rules = new GameRules(
            rosettePositions: new HashSet<int> { 3 },
            piecesPerPlayer: 5,
            pathLength: 10,
            sharedLaneStart: 3,
            sharedLaneEnd: 7);

        await Assert.That(rules.PiecesPerPlayer).IsEqualTo(5);
        await Assert.That(rules.PathLength).IsEqualTo(10);
        await Assert.That(rules.IsRosette(3)).IsTrue();
        await Assert.That(rules.IsRosette(4)).IsFalse();
    }
}
