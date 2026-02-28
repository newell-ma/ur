using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Engine.Tests;

public class GreedyAiPlayerTests
{
    private static readonly GreedyAiPlayer Ai = new("AI", thinkingDelay: TimeSpan.Zero);

    #region Bear Off Priority

    [Test]
    public async Task PrefersBearOff_OverRosette()
    {
        // Piece at 13 can bear off (to 15), piece at 12 can land on rosette 14
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 13)
            .WithPiece(Player.One, 12)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, 13, 15), // bear off (piece index 0)
            new(Player.One, 1, 12, 14), // rosette (piece index 1)
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen.To).IsEqualTo(15);
    }

    #endregion

    #region Rosette Priority

    [Test]
    public async Task PrefersRosette_OverPlainMove()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 2)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, 2, 4), // rosette at 4 (piece index 0)
            new(Player.One, 1, 5, 7), // plain move (piece index 1)
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen.To).IsEqualTo(4);
    }

    [Test]
    public async Task PrefersRosette_OverCapture()
    {
        // Rosette is more valuable than capture
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 2)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, 2, 4), // rosette (piece index 0)
            new(Player.One, 1, 5, 7), // capture (piece index 1)
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen.To).IsEqualTo(4);
    }

    #endregion

    #region Capture Priority

    [Test]
    public async Task PrefersCapture_OverPlainAdvance()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.One, 9)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, 5, 7), // capture (piece index 0)
            new(Player.One, 1, 9, 11), // plain advance (piece index 1)
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen.To).IsEqualTo(7);
    }

    #endregion

    #region Valid Move Returned

    [Test]
    public async Task AlwaysReturnsValidMove_SingleOption()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 3)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move> { new(Player.One, 0, 3, 5) };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen).IsEqualTo(moves[0]);
    }

    [Test]
    public async Task AlwaysReturnsValidMove_FromProvidedList()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, -1)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.One, 10)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, -1, 2),
            new(Player.One, 1, 5, 8),  // rosette at 8 (piece index 1)
            new(Player.One, 2, 10, 13),
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 3);
        await Assert.That(moves).Contains(chosen);
    }

    #endregion

    #region Advance vs Enter

    [Test]
    public async Task PrefersFurthestAdvance_OverEnterFromStart()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, -1)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();

        var moves = new List<Move>
        {
            new(Player.One, 0, -1, 1),  // enter from start (piece index 0)
            new(Player.One, 1, 5, 7),   // advance on-board piece (piece index 1)
        };

        var chosen = await Ai.ChooseMoveAsync(state, moves, 2);
        await Assert.That(chosen.From).IsEqualTo(5);
    }

    #endregion
}
