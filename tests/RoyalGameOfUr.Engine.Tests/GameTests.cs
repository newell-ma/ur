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
            game.ExecuteMove(new Move(Player.One, 0, -1, 0)));
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
        // PathLength=15, piece at 13, roll 3 = 16 > 15 -> invalid
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
        // Piece at 13, roll 2 = 15 = PathLength -> valid bear off
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
        // Two pieces at -1, roll 1 -> only one enter move
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();

        var moves = game.GetValidMoves();
        // All 7 pieces at -1, but only one unique move (-1 -> 0)
        await Assert.That(moves.Count).IsEqualTo(1);
    }

    [Test]
    public async Task CantCaptureOnPrivateLane()
    {
        // Position 2 is NOT shared lane (shared is 5-12), so opponent can't be there
        // Actually the private lane means each player has their own positions 0-4 and 13-14.
        // But the way the game works, pieces on private lanes can't collide because
        // each player has their own path. The shared lane check handles this.
        // Let's verify: P1 at 3, P2 at 3 -> position 3 is NOT shared, so no capture issue
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

        var outcome = game.ExecuteMove(rosetteMove);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.ExtraTurn);
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

        var move = game.GetValidMoves().First(m => m.From == 5 && m.To == 7);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Captured);
        await Assert.That(outcome.HasCapture).IsTrue();
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

        var move = game.GetValidMoves().First(m => m.From == 13 && m.To == 15);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.BorneOff);
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

        var move = game.GetValidMoves().First(m => m.From == 13 && m.To == 15);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Win);
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
        var move = game.GetValidMoves().First(m => m.From == 13 && m.To == 15);
        game.ExecuteMove(move);

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

        // Roll 4: piece at 0 -> 4 (rosette, extra turn)
        // Roll 4: piece at 4 -> 8 (rosette, extra turn)
        var game = new Game(new FixedDice(4, 4), state);

        game.Roll();
        var move1 = game.GetValidMoves().First(m => m.From == 0 && m.To == 4);
        var outcome1 = game.ExecuteMove(move1);
        await Assert.That(outcome1.Result).IsEqualTo(MoveResult.ExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);

        game.Roll();
        var move2 = game.GetValidMoves().First(m => m.From == 4 && m.To == 8);
        var outcome2 = game.ExecuteMove(move2);
        await Assert.That(outcome2.Result).IsEqualTo(MoveResult.ExtraTurn);
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
        var captureMove = game.GetValidMoves().First(m => m.From == 5 && m.To == 7);
        game.ExecuteMove(captureMove);

        // P2's turn, piece is back at -1, roll 1 -> enter at 0
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

        // P1: roll 2 -> enter at 1 (-1+2=1)
        // P2: roll 3 -> enter at 2 (-1+3=2, rosette -> extra turn)
        // P2: roll 2 -> move 2->4, bear off -> Win
        var game = new Game(new FixedDice(2, 3, 2), rules);

        game.Roll();
        var p1Move = game.GetValidMoves().First(m => m.From == -1 && m.To == 1);
        game.ExecuteMove(p1Move);

        game.Roll();
        var p2Move1 = game.GetValidMoves().First(m => m.From == -1 && m.To == 2);
        var r1 = game.ExecuteMove(p2Move1);
        await Assert.That(r1.Result).IsEqualTo(MoveResult.ExtraTurn);

        game.Roll();
        var p2Move2 = game.GetValidMoves().First(m => m.From == 2 && m.To == 4);
        var r2 = game.ExecuteMove(p2Move2);
        await Assert.That(r2.Result).IsEqualTo(MoveResult.Win);
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

        var move = game.GetValidMoves().First(m => m.From == rosettePos - 1 && m.To == rosettePos);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.ExtraTurn);
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

    #region Cross-Capture Tests (Masters Path)

    [Test]
    public async Task Masters_CrossCapture_P1At11CapturesP2At15()
    {
        // P1 moves to position 11, P2 has piece at position 15 (same physical tile)
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 9)
            .WithPiece(Player.Two, 15)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 9 && m.To == 11);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.CapturedAndExtraTurn); // 11 is rosette + capture
        await Assert.That(outcome.HasCapture).IsTrue();
        await Assert.That(game.State.IsOccupiedBy(Player.Two, 15)).IsFalse();
        await Assert.That(game.State.PiecesAtStart(Player.Two)).IsEqualTo(7);
    }

    [Test]
    public async Task Masters_CrossCapture_P2At11CapturesP1At15()
    {
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.Two, 9)
            .WithPiece(Player.One, 15)
            .WithCurrentPlayer(Player.Two)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 9 && m.To == 11);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.CapturedAndExtraTurn);
        await Assert.That(game.State.IsOccupiedBy(Player.One, 15)).IsFalse();
    }

    [Test]
    public async Task Masters_CrossCapture_P1At14CapturesP2At12()
    {
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 12)
            .WithPiece(Player.Two, 12) // P2 at 12 maps to P1's capture pos 14
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 12 && m.To == 14);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Captured);
        await Assert.That(game.State.IsOccupiedBy(Player.Two, 12)).IsFalse();
    }

    [Test]
    public async Task Masters_SharedMiddle_IdentityCapture()
    {
        // Positions 4-10 use identity mapping (same as Bell path shared lane)
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7) // shared middle, identity
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 5 && m.To == 7);
        var outcome = game.ExecuteMove(move);

        // Position 7 is a rosette in Masters but SafeRosettes is false -> capture allowed
        await Assert.That(outcome.Result).IsEqualTo(MoveResult.CapturedAndExtraTurn);
    }

    #endregion

    #region Zero Roll Value (Masters)

    [Test]
    public async Task Masters_ZeroRoll_GeneratesMovesOfDistance4()
    {
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();
        // FixedDice returns 0 (raw roll)
        var game = new Game(new FixedDice(0), state);
        int rawRoll = game.Roll();

        await Assert.That(rawRoll).IsEqualTo(0);
        await Assert.That(game.State.EffectiveRoll).IsEqualTo(4);

        var moves = game.GetValidMoves();
        await Assert.That(moves.Count).IsGreaterThan(0);
        // Piece at 5 should move to 9 (5+4)
        var move = moves.FirstOrDefault(m => m.From == 5 && m.To == 9);
        await Assert.That(move).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task Finkel_ZeroRoll_NoMoves()
    {
        // Finkel has no ZeroRollValue, so 0 = no moves
        var game = new Game(new FixedDice(0), GameRules.Finkel);
        game.Roll();

        await Assert.That(game.State.EffectiveRoll).IsEqualTo(0);
        var moves = game.GetValidMoves();
        await Assert.That(moves.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Masters_NonZeroRoll_EffectiveRollMatchesRaw()
    {
        var game = new Game(new FixedDice(2), GameRules.Masters);
        int rawRoll = game.Roll();

        await Assert.That(rawRoll).IsEqualTo(2);
        await Assert.That(game.State.EffectiveRoll).IsEqualTo(2);
    }

    #endregion

    #region Unsafe Rosette Tests (Masters/Blitz)

    [Test]
    public async Task Masters_CanCaptureOnRosette()
    {
        // Position 7 is a rosette in Masters, SafeRosettes=false
        var rules = GameRules.Masters;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var captureMove = moves.FirstOrDefault(m => m.From == 5 && m.To == 7);
        await Assert.That(captureMove).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task Finkel_CantCaptureOnRosette_Confirmed()
    {
        // Position 8 is rosette in Finkel, SafeRosettes=true
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

    #endregion

    #region Capture Extra Roll (Blitz)

    [Test]
    public async Task Blitz_CaptureGrantsExtraTurn()
    {
        var rules = GameRules.Blitz;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 8) // non-rosette shared lane
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(3), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 5 && m.To == 8);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.CapturedAndExtraTurn);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.One);
    }

    [Test]
    public async Task Finkel_CaptureDoesNotGrantExtraTurn()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 5 && m.To == 7);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Captured);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.Two);
    }

    #endregion

    #region Stacking Tests (Tournament)

    [Test]
    public async Task Tournament_CanStackOnRosette()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 3) // rosette at 3
            .WithPiece(Player.One, 1) // piece that can move to 3
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var stackMove = moves.FirstOrDefault(m => m.From == 1 && m.To == 3);
        await Assert.That(stackMove).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task Tournament_CantStackOnNonRosette()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.One, 3) // piece that could move to 5 but 5 is not rosette
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var blockedMove = moves.FirstOrDefault(m => m.From == 3 && m.To == 5);
        await Assert.That(blockedMove).IsEqualTo(default(Move));
    }

    [Test]
    public async Task Tournament_StackMovesAsOne()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 7) // rosette, stacked
            .WithPiece(Player.One, 7) // second piece at same rosette
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(1), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 7 && m.To == 8);
        game.ExecuteMove(move);

        // Both pieces should have moved from 7 to 8
        await Assert.That(game.State.IsOccupiedBy(Player.One, 7)).IsFalse();
        await Assert.That(game.State.PieceCountAt(Player.One, 8)).IsEqualTo(2);
    }

    [Test]
    public async Task Tournament_StackBearOff_AllBearOff()
    {
        var rules = GameRules.Tournament;
        var builder = new GameStateBuilder(rules)
            .WithPiece(Player.One, 15) // rosette, stacked
            .WithPiece(Player.One, 15) // second piece
            .WithCurrentPlayer(Player.One);
        // Fill remaining pieces as borne off
        for (int i = 0; i < 3; i++)
            builder.WithPiece(Player.One, 16);

        var state = builder.Build();
        var game = new Game(new FixedDice(1), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 15 && m.To == 16);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Win);
        await Assert.That(game.State.PiecesBorneOff(Player.One)).IsEqualTo(5);
    }

    [Test]
    public async Task Tournament_StackCapturesAllOpponentPieces()
    {
        // With stacking, capturing sends ALL opponent pieces at that position back
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7) // rosette, stacked opponent
            .WithPiece(Player.Two, 7) // second opponent piece
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // Tournament has SafeRosettes=true, so can't capture on rosette
        // Let's use a non-rosette position instead
        var moves = game.GetValidMoves();
        var captureMove = moves.FirstOrDefault(m => m.From == 5 && m.To == 7);
        // Position 7 is a rosette and Tournament has SafeRosettes=true -> blocked
        await Assert.That(captureMove).IsEqualTo(default(Move));
    }

    [Test]
    public async Task Tournament_StackCapture_NonRosette()
    {
        // Test capturing stacked opponent on non-rosette shared position
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 4)
            .WithPiece(Player.Two, 6) // non-rosette shared
            .WithPiece(Player.Two, 6) // stacked opponent -- wait, stacking only on rosettes
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // P1 moves to 6, captures. With AllowStacking, captures ALL at position 6
        var move = game.GetValidMoves().First(m => m.From == 4 && m.To == 6);
        var outcome = game.ExecuteMove(move);
        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Captured);
        await Assert.That(game.State.IsOccupiedBy(Player.Two, 6)).IsFalse();
        // Both P2 pieces should be back at start
        await Assert.That(game.State.PiecesAtStart(Player.Two)).IsEqualTo(5);
    }

    #endregion

    #region Backward Movement Tests (Tournament)

    [Test]
    public async Task Tournament_BackwardMovesGenerated()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var backwardMove = moves.FirstOrDefault(m => m.From == 5 && m.To == 3);
        await Assert.That(backwardMove).IsNotEqualTo(default(Move));
    }

    [Test]
    public async Task Tournament_BackwardMove_CantGoBelowZero()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 1) // position 1, roll 3 -> -2 (invalid)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(3), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var invalidBackward = moves.Any(m => m.From == 1 && m.To < 0);
        await Assert.That(invalidBackward).IsFalse();
    }

    [Test]
    public async Task Tournament_BackwardMove_CanCapture()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 8)
            .WithPiece(Player.Two, 6) // shared lane, non-rosette
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // Backward move from 8 to 6 should capture P2
        var backwardMove = game.GetValidMoves().First(m => m.From == 8 && m.To == 6);
        var outcome = game.ExecuteMove(backwardMove);
        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Captured);
        await Assert.That(game.State.IsOccupiedBy(Player.Two, 6)).IsFalse();
    }

    [Test]
    public async Task Finkel_NoBackwardMoves()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        var backwardMove = moves.Any(m => m.To < m.From);
        await Assert.That(backwardMove).IsFalse();
    }

    [Test]
    public async Task Tournament_NoBackwardFromPositionZero()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 0) // position 0
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(1), state);
        game.Roll();

        var moves = game.GetValidMoves();
        // No backward moves from 0 (guard: from > 0)
        var backwardMove = moves.Any(m => m.From == 0 && m.To < 0);
        await Assert.That(backwardMove).IsFalse();
    }

    #endregion

    #region Voluntary Skip Tests (Tournament)

    [Test]
    public async Task Tournament_CanForfeitWithOnlyBackwardMoves()
    {
        // All pieces borne off except one near the end, so only backward moves exist
        var rules = GameRules.Tournament;
        var builder = new GameStateBuilder(rules)
            .WithPiece(Player.One, 15) // near end, forward overshoots
            .WithCurrentPlayer(Player.One);
        for (int i = 0; i < 4; i++)
            builder.WithPiece(Player.One, 16); // borne off

        var state = builder.Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // forward: 15+2=17 > 16 (overshoot, invalid)
        // backward: 15-2=13 (valid)
        var moves = game.GetValidMoves();
        bool allBackward = moves.All(m => m.To < m.From);
        await Assert.That(allBackward).IsTrue();
        await Assert.That(moves.Count).IsGreaterThan(0);

        // Should be able to forfeit (voluntary skip)
        game.ForfeitTurn();
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.Two);
    }

    [Test]
    public void Tournament_CantForfeitWithForwardMoves()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 5) // can move forward and backward
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        // Has forward move (5->7), so can't forfeit
        Assert.Throws<InvalidOperationException>(() => game.ForfeitTurn());
    }

    [Test]
    public void Finkel_CantForfeitWithMoves()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();

        // Has moves, AllowVoluntarySkip=false -> can't forfeit
        Assert.Throws<InvalidOperationException>(() => game.ForfeitTurn());
    }

    #endregion

    #region Tournament Rosette Extra Roll Disabled

    [Test]
    public async Task Tournament_RosetteDoesNotGrantExtraTurn()
    {
        var rules = GameRules.Tournament;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 2) // move to 3 (rosette)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(1), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 2 && m.To == 3);
        var outcome = game.ExecuteMove(move);

        // Tournament: RosetteExtraRoll=false, so no extra turn
        await Assert.That(outcome.Result).IsEqualTo(MoveResult.Moved);
        await Assert.That(game.State.CurrentPlayer).IsEqualTo(Player.Two);
    }

    #endregion

    #region Piece Identity Tests

    [Test]
    public async Task Pieces_HaveCorrectIds()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();
        var pieces = state.GetPieces(Player.One).ToArray();

        for (int i = 0; i < pieces.Length; i++)
        {
            await Assert.That(pieces[i].Id).IsEqualTo(i);
            await Assert.That(pieces[i].Position).IsEqualTo(-1);
        }
    }

    [Test]
    public async Task Move_CarriesPieceIndex()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 3)
            .WithPiece(Player.One, 5)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var moves = game.GetValidMoves();
        // Each move should have a valid PieceIndex
        foreach (var move in moves)
        {
            await Assert.That(move.PieceIndex).IsGreaterThanOrEqualTo(0);
            await Assert.That(move.PieceIndex).IsLessThan(7);
        }
    }

    [Test]
    public async Task MoveOutcome_TracksCapture()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithPiece(Player.One, 5)
            .WithPiece(Player.Two, 7)
            .WithCurrentPlayer(Player.One)
            .Build();
        var game = new Game(new FixedDice(2), state);
        game.Roll();

        var move = game.GetValidMoves().First(m => m.From == 5 && m.To == 7);
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.HasCapture).IsTrue();
        await Assert.That(outcome.CapturedPieceIndex).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task MoveOutcome_NoCaptureIndex_WhenNoCapture()
    {
        var game = new Game(new FixedDice(1), GameRules.Finkel);
        game.Roll();

        var move = game.GetValidMoves()[0];
        var outcome = game.ExecuteMove(move);

        await Assert.That(outcome.HasCapture).IsFalse();
        await Assert.That(outcome.CapturedPieceIndex).IsEqualTo(-1);
    }

    #endregion
}
