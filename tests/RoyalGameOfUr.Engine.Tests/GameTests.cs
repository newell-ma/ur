using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Engine.Tests;

public class GameTests
{
    #region Initial State

    [Test]
    public async Task NewGame_AllPiecesAtStart()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        var state = game.State;

        await Assert.That(state.PiecesAtStart(Player.One)).IsEqualTo(7);
        await Assert.That(state.PiecesAtStart(Player.Two)).IsEqualTo(7);
        await Assert.That(state.PiecesOnBoard(Player.One)).IsEqualTo(0);
        await Assert.That(state.PiecesOnBoard(Player.Two)).IsEqualTo(0);
    }

    [Test]
    public async Task NewGame_PlayerOneStarts()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);
    }

    [Test]
    public async Task NewGame_NoWinner()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        await Assert.That(game.State.IsGameOver).IsFalse();
        await Assert.That(game.State.Winner).IsNull();
    }

    [Test]
    public async Task NewGame_PieceCounts()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        await Assert.That(game.State.PiecesAtStart(Player.One)).IsEqualTo(7);
        await Assert.That(game.State.PiecesBorneOff(Player.One)).IsEqualTo(0);
        await Assert.That(game.State.PiecesOnBoard(Player.One)).IsEqualTo(0);
    }

    #endregion

    #region Roll Protocol

    [Test]
    public void Roll_CalledTwice_Throws()
    {
        var game = new Game(new FixedDice(1, 2), GameRules.Finkel);
        game.Roll();

        Assert.Throws<InvalidOperationException>(() => game.Roll());
    }

    [Test]
    public void GetValidMoves_BeforeRoll_Throws()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);

        Assert.Throws<InvalidOperationException>(() => game.GetValidMoves());
    }

    [Test]
    public void ExecuteMove_BeforeRoll_Throws()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);

        Assert.Throws<InvalidOperationException>(() =>
            game.ExecuteMove(new Move(Player.One, -1, 0)));
    }

    [Test]
    public void ForfeitTurn_BeforeRoll_Throws()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);

        Assert.Throws<InvalidOperationException>(() => game.ForfeitTurn());
    }

    #endregion

    #region Move Generation

    [Test]
    public async Task Roll0_NoValidMoves()
    {
        var game = new Game(new FixedDice(0), GameRules.Finkel);
        game.Roll();

        var moves = game.GetValidMoves();
        await Assert.That(moves.Count).IsEqualTo(0);
    }

    [Test]
    public async Task EnterMove_FromStartToPosition()
    {
        var game = new Game(new FixedDice(3), GameRules.Finkel);
        game.Roll();

        var moves = game.GetValidMoves();
        await Assert.That(moves.Count).IsEqualTo(1);
        await Assert.That(moves[0].From).IsEqualTo(-1);
        await Assert.That(moves[0].To).IsEqualTo(2); // -1 + 3 = 2
    }

    [Test]
    public async Task CantLandOnOwnPiece()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 3) // piece at position 3
            .WithPiece(Player.One, 1) // piece at position 1
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();

        // Piece at 1 can't move to 3 (own piece), piece at 3 can move to 5
        // Pieces at -1 can move to 1? No, there's a piece at 1. So -1 can't go to 1.
        // Actually -1+2=1, occupied. 1+2=3, occupied. 3+2=5, ok.
        bool hasBlockedMove = moves.Any(m => m.To == 3 && m.From == 1);
        await Assert.That(hasBlockedMove).IsFalse();
    }

    [Test]
    public async Task CanCaptureOnNonRosette()
    {
        // Position 5 is shared lane, not a rosette in Finkel
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 3)
            .WithPiece(Player.Two, 5)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var captureMove = moves.FirstOrDefault(m => m.From == 3 && m.To == 5);
        await Assert.That(captureMove).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task CantCaptureOnRosette()
    {
        // Position 8 is rosette in Finkel and in shared lane
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 6)
            .WithPiece(Player.Two, 8)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var captureMove = moves.FirstOrDefault(m => m.From == 6 && m.To == 8);
        await Assert.That(captureMove).IsEqualTo(default(Move));
    }

    [Test]
    public async Task ExactBearOffRequired()
    {
        // PathLength=15, piece at 13, roll 3 = 16 > 15 → invalid
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 13)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(3), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var overshoot = moves.Any(m => m.From == 13);
        await Assert.That(overshoot).IsFalse();
    }

    [Test]
    public async Task ExactBearOff_Allowed()
    {
        // Piece at 13, roll 2 = 15 = PathLength → valid bear off
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 13)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var bearOff = moves.FirstOrDefault(m => m.From == 13 && m.To == 15);
        await Assert.That(bearOff).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task Deduplication_MultiplePiecesAtSamePosition()
    {
        // Two pieces at -1, roll 1 → only one enter move
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();

        var moves = game.GetValidMoves();
        // All 7 pieces at -1, but only one unique move (-1 → 0)
        await Assert.That(moves.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CantCaptureOnPrivateLane()
    {
        // Position 2 is NOT shared lane (shared is 5-12), so opponent can't be there
        // Actually the private lane means each player has their own positions 0-4 and 13-14.
        // But the way the game works, pieces on private lanes can't collide because
        // each player has their own path. The shared lane check handles this.
        // Let's verify: P1 at 3, P2 at 3 → position 3 is NOT shared, so no capture issue
        // They're on different physical squares even though same index
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 3)
            .WithPiece(Player.Two, 3)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // P1's piece at 3 can move to 5 (shared lane) - should be valid
        var moves = game.GetValidMoves();
        var moveToShared = moves.FirstOrDefault(m => m.From == 3 && m.To == 5);
        await Assert.That(moveToShared).IsNotEqualTo(default(Move));
    }

    #endregion

    #region Move Execution

    [Test]
    public async Task NormalMove_SwitchesTurn()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();
        var moves = game.GetValidMoves();
        game.ExecuteMove(moves[0]);

        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.Two);
    }

    [Test]
    public async Task RosetteMove_GrantsExtraTurn()
    {
        // Roll 4 lands on position 3 (-1+4=3)... that's not a rosette.
        // Rosette at 4: need to land on 4. From -1, roll 5 = position 4. But max roll is 4.
        // From 0, roll 4 = position 4. So enter first, then move to rosette.
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 0)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(4), state);
        game.Roll();
        var moves = game.GetValidMoves();
        var rosetteMove = moves.First(m => m.To == 4);

        var result = game.ExecuteMove(rosetteMove);

        await Assert.That(result).IsEqualTo(MoveResult.ExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);
    }

    [Test]
    public async Task Capture_SendsPieceToStart()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = new Move(Player.One, 5, 7);
        var result = game.ExecuteMove(move);

        await Assert.That(result).IsEqualTo(MoveResult.Captured);
        // P2's piece should be back at start
        await Assert.That(game.State.IsOccupiedBy(Player.Two, 7)).IsFalse();
        await Assert.That(game.State.PiecesAtStart(Player.Two)).IsEqualTo(7); // all back including captured
    }

    [Test]
    public async Task BearOff_Works()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 13)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = new Move(Player.One, 13, 15);
        var result = game.ExecuteMove(move);

        await Assert.That(result).IsEqualTo(MoveResult.BorneOff);
        await Assert.That(game.State.PiecesBorneOff(Player.One)).IsEqualTo(1);
    }

    [Test]
    public async Task LastBearOff_Wins()
    {
        // Set up with all pieces borne off except one
        var builder = new GameStateBuilder(GameRules.Finkel)
            .WithCurrentPlayer(Player.One);
        for (int i = 0; i < 6; i++)
            builder.WithPiece(Player.One, 15); // borne off
        builder.WithPiece(Player.One, 13); // last piece near end

        var state = builder.Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = new Move(Player.One, 13, 15);
        var result = game.ExecuteMove(move);

        await Assert.That(result).IsEqualTo(MoveResult.Win);
        await Assert.That(game.State.Winner).IsEqualTo(Player.One);
        await Assert.That(game.State.IsGameOver).IsTrue();
    }

    [Test]
    public void Roll_AfterGameOver_Throws()
    {
        var builder = new GameStateBuilder(GameRules.Finkel)
            .WithCurrentPlayer(Player.One);
        for (int i = 0; i < 6; i++)
            builder.WithPiece(Player.One, 15);
        builder.WithPiece(Player.One, 13);

        var state = builder.Build();
        var game = new Game(new FixedDice(2, 1), state);
        game.Roll();
        game.ExecuteMove(new Move(Player.One, 13, 15));

        Assert.Throws<InvalidOperationException>(() => game.Roll());
    }

    #endregion

    #region Forfeit

    [Test]
    public async Task Forfeit_Roll0_PassesTurn()
    {
        var game = new Game(new FixedDice(0), GameRules.Finkel);
        game.Roll();
        game.ForfeitTurn();

        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.Two);
    }

    [Test]
    public void Forfeit_WhenMovesExist_Throws()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();

        Assert.Throws<InvalidOperationException>(() => game.ForfeitTurn());
    }

    #endregion

    #region Integration Scenarios

    [Test]
    public async Task RosetteChain_MultipleExtraTurns()
    {
        // Player gets rosette, rolls again, gets another rosette
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 0)
            .WithCurrentPlayer(Player.One)
            .Build();

        // Roll 4: piece at 0 → 4 (rosette, extra turn)
        // Roll 4: piece at 4 → 8 (rosette, extra turn)
        var game = new Game(new FixedDice(4, 4), state);

        game.Roll();
        var result1 = game.ExecuteMove(new Move(Player.One, 0, 4));
        await Assert.That(result1).IsEqualTo(MoveResult.ExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);

        game.Roll();
        var result2 = game.ExecuteMove(new Move(Player.One, 4, 8));
        await Assert.That(result2).IsEqualTo(MoveResult.ExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);
    }

    [Test]
    public async Task CaptureAndReenter()
    {
        // P1 captures P2, P2 must re-enter
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2, 1), state);

        // P1 captures P2 at position 7
        game.Roll();
        game.ExecuteMove(new Move(Player.One, 5, 7));

        // P2's turn, piece is back at -1, roll 1 → enter at 0
        game.Roll();
        var moves = game.GetValidMoves();
        await Assert.That(moves.Count).IsEqualTo(1);
        await Assert.That(moves[0].From).IsEqualTo(-1);
        await Assert.That(moves[0].To).IsEqualTo(0);
    }

    [Test]
    public async Task FullScriptedMiniGame()
    {
        // Use simple custom rules for a shorter game
        var rules = new GameRules(
            rosettePositions: new HashSet<int> { 2 },
            piecesPerPlayer: 1,
            pathLength: 4,
            sharedLaneStart: 2,
            sharedLaneEnd: 3);

        // P1: roll 2 → enter at 1 (-1+2=1)
        // P2: roll 3 → enter at 2 (-1+3=2, rosette → extra turn)
        // P2: roll 2 → move 2→4, bear off → Win
        var game = new Game(new FixedDice(2, 3, 2), rules);

        game.Roll();
        game.ExecuteMove(new Move(Player.One, -1, 1));

        game.Roll();
        var r1 = game.ExecuteMove(new Move(Player.Two, -1, 2));
        await Assert.That(r1).IsEqualTo(MoveResult.ExtraTurn);

        game.Roll();
        var r2 = game.ExecuteMove(new Move(Player.Two, 2, 4));
        await Assert.That(r2).IsEqualTo(MoveResult.Win);
        await Assert.That(game.State.Winner).IsEqualTo(Player.Two);
    }

    #endregion

    #region Parameterized Tests

    [Test]
    [Arguments(1, 0)]
    [Arguments(2, 1)]
    [Arguments(3, 2)]
    [Arguments(4, 3)]
    public async Task EnterFromStart_LandsAtCorrectPosition(int roll, int expectedTo)
    {
        var game = new Game(new FixedDice(roll), GameRules.Finkel);
        game.Roll();
        var moves = game.GetValidMoves();

        await Assert.That(moves[0].From).IsEqualTo(-1);
        await Assert.That(moves[0].To).IsEqualTo(expectedTo);
    }

    [Test]
    [Arguments(4)]
    [Arguments(8)]
    [Arguments(14)]
    public async Task LandingOnRosette_GrantsExtraTurn(int rosettePos)
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, rosettePos - 1)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(1), state);
        game.Roll();

        var result = game.ExecuteMove(new Move(Player.One, rosettePos - 1, rosettePos));

        await Assert.That(result).IsEqualTo(MoveResult.ExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);
    }

    #endregion

    #region GameStateBuilder Tests

    [Test]
    public async Task Builder_DefaultState_AllAtStart()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        await Assert.That(state.PiecesAtStart(Player.One)).IsEqualTo(7);
        await Assert.That(state.PiecesAtStart(Player.Two)).IsEqualTo(7);
        await Assert.That(state.CurrentPlayer).IsEqualTo(Player.One);
    }

    [Test]
    public async Task Builder_WithPieces_PositionsCorrect()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.One, 10)
            .WithCurrentPlayer(Player.Two)
            .Build();

        await Assert.That(state.IsOccupiedBy(Player.One, 5)).IsTrue();
        await Assert.That(state.IsOccupiedBy(Player.One, 10)).IsTrue();
        await Assert.That(state.PiecesOnBoard(Player.One)).IsEqualTo(2);
        await Assert.That(state.PiecesAtStart(Player.One)).IsEqualTo(5);
        await Assert.That(state.CurrentPlayer).IsEqualTo(Player.Two);
    }

    #endregion
}
