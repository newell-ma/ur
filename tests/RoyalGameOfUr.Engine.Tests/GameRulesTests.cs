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

    #region Masters Preset

    [Test]
    public async Task Masters_HasCorrectProperties()
    {
        var rules = GameRules.Masters;

        await Assert.That(rules.Name).IsEqualTo("Masters");
        await Assert.That(rules.PiecesPerPlayer).IsEqualTo(7);
        await Assert.That(rules.PathLength).IsEqualTo(16);
        await Assert.That(rules.DiceCount).IsEqualTo(3);
        await Assert.That(rules.SafeRosettes).IsFalse();
        await Assert.That(rules.RosetteExtraRoll).IsTrue();
        await Assert.That(rules.CaptureExtraRoll).IsFalse();
        await Assert.That(rules.ZeroRollValue).IsEqualTo(4);
        await Assert.That(rules.AllowStacking).IsFalse();
        await Assert.That(rules.AllowBackwardMoves).IsFalse();
        await Assert.That(rules.AllowVoluntarySkip).IsFalse();
    }

    [Test]
    public async Task Masters_HasCorrectRosettes()
    {
        var rules = GameRules.Masters;

        await Assert.That(rules.RosettePositions).Contains(3);
        await Assert.That(rules.RosettePositions).Contains(7);
        await Assert.That(rules.RosettePositions).Contains(11);
        await Assert.That(rules.RosettePositions).Contains(15);
        await Assert.That(rules.RosettePositions.Count).IsEqualTo(4);
    }

    [Test]
    public async Task Masters_CaptureMap_IdentityForSharedMiddle()
    {
        var rules = GameRules.Masters;

        for (int i = 4; i <= 10; i++)
        {
            await Assert.That(rules.GetOpponentCapturePosition(i)).IsEqualTo(i);
        }
    }

    [Test]
    public async Task Masters_CaptureMap_CrossZone()
    {
        var rules = GameRules.Masters;

        await Assert.That(rules.GetOpponentCapturePosition(11)).IsEqualTo(15);
        await Assert.That(rules.GetOpponentCapturePosition(12)).IsEqualTo(14);
        await Assert.That(rules.GetOpponentCapturePosition(13)).IsEqualTo(13);
        await Assert.That(rules.GetOpponentCapturePosition(14)).IsEqualTo(12);
        await Assert.That(rules.GetOpponentCapturePosition(15)).IsEqualTo(11);
    }

    #endregion

    #region Blitz Preset

    [Test]
    public async Task Blitz_HasCorrectProperties()
    {
        var rules = GameRules.Blitz;

        await Assert.That(rules.Name).IsEqualTo("Blitz");
        await Assert.That(rules.PiecesPerPlayer).IsEqualTo(5);
        await Assert.That(rules.PathLength).IsEqualTo(16);
        await Assert.That(rules.DiceCount).IsEqualTo(4);
        await Assert.That(rules.SafeRosettes).IsFalse();
        await Assert.That(rules.RosetteExtraRoll).IsTrue();
        await Assert.That(rules.CaptureExtraRoll).IsTrue();
        await Assert.That(rules.ZeroRollValue).IsNull();
        await Assert.That(rules.AllowStacking).IsFalse();
    }

    #endregion

    #region Tournament Preset

    [Test]
    public async Task Tournament_HasCorrectProperties()
    {
        var rules = GameRules.Tournament;

        await Assert.That(rules.Name).IsEqualTo("Tournament");
        await Assert.That(rules.PiecesPerPlayer).IsEqualTo(5);
        await Assert.That(rules.PathLength).IsEqualTo(16);
        await Assert.That(rules.DiceCount).IsEqualTo(4);
        await Assert.That(rules.SafeRosettes).IsTrue();
        await Assert.That(rules.RosetteExtraRoll).IsFalse();
        await Assert.That(rules.CaptureExtraRoll).IsFalse();
        await Assert.That(rules.AllowStacking).IsTrue();
        await Assert.That(rules.AllowBackwardMoves).IsTrue();
        await Assert.That(rules.AllowVoluntarySkip).IsTrue();
    }

    #endregion

    #region CaptureMap Auto-Build

    [Test]
    public async Task Finkel_CaptureMap_IsIdentity()
    {
        var rules = GameRules.Finkel;

        for (int i = 5; i <= 12; i++)
        {
            await Assert.That(rules.IsSharedLane(i)).IsTrue();
            await Assert.That(rules.GetOpponentCapturePosition(i)).IsEqualTo(i);
        }

        await Assert.That(rules.IsSharedLane(4)).IsFalse();
        await Assert.That(rules.IsSharedLane(13)).IsFalse();
    }

    [Test]
    public async Task CustomRules_CaptureMap_AutoBuiltFromSharedLane()
    {
        var rules = new GameRules(
            rosettePositions: new HashSet<int> { 2 },
            piecesPerPlayer: 1,
            pathLength: 4,
            sharedLaneStart: 2,
            sharedLaneEnd: 3);

        await Assert.That(rules.IsSharedLane(2)).IsTrue();
        await Assert.That(rules.IsSharedLane(3)).IsTrue();
        await Assert.That(rules.IsSharedLane(1)).IsFalse();
        await Assert.That(rules.GetOpponentCapturePosition(2)).IsEqualTo(2);
        await Assert.That(rules.GetOpponentCapturePosition(3)).IsEqualTo(3);
    }

    #endregion
}
