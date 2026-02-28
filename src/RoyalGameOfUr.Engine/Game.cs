namespace RoyalGameOfUr.Engine;

public sealed class Game
{
    private readonly IDice _dice;
    private bool _hasRolled;

    public GameState State { get; }

    public Game(IDice dice, GameRules rules)
    {
        _dice = dice;
        State = new GameStateBuilder(rules).Build();
    }

    public Game(IDice dice, GameState state)
    {
        _dice = dice;
        State = state;
    }

    public int Roll()
    {
        if (State.IsGameOver)
            throw new InvalidOperationException("Game is over.");
        if (_hasRolled)
            throw new InvalidOperationException("Already rolled this turn.");

        int roll = _dice.Roll();
        State.LastRoll = roll;

        int effectiveRoll = roll == 0 && State.Rules.ZeroRollValue is { } zeroVal
            ? zeroVal
            : roll;
        State.EffectiveRoll = effectiveRoll;

        _hasRolled = true;
        return roll;
    }

    public IReadOnlyList<Move> GetValidMoves()
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before getting moves.");

        int roll = State.EffectiveRoll;
        if (roll == 0)
            return Array.Empty<Move>();

        var player = State.CurrentPlayer;
        var pieces = State.GetPieces(player);
        var rules = State.Rules;
        var moves = new List<Move>();
        var seen = new HashSet<(int from, int to)>();

        for (int idx = 0; idx < pieces.Length; idx++)
        {
            int from = pieces[idx].Position;
            if (from == rules.PathLength) continue; // already borne off

            // Forward move
            int forwardTo = from + roll;
            if (forwardTo <= rules.PathLength && IsValidDestination(player, forwardTo, rules))
            {
                if (seen.Add((from, forwardTo)))
                    moves.Add(new Move(player, idx, from, forwardTo));
            }

            // Backward move
            if (rules.AllowBackwardMoves && from > 0)
            {
                int backwardTo = from - roll;
                if (backwardTo >= 0 && IsValidDestination(player, backwardTo, rules))
                {
                    if (seen.Add((from, backwardTo)))
                        moves.Add(new Move(player, idx, from, backwardTo));
                }
            }
        }

        return moves;
    }

    private bool IsValidDestination(Player player, int to, GameRules rules)
    {
        if (to == rules.PathLength)
            return true; // bearing off is always valid

        // Check own piece blocking
        if (State.IsOccupiedBy(player, to))
        {
            if (!rules.AllowStacking || !rules.IsRosette(to))
                return false; // can't land on own piece (unless stacking on rosette)
        }

        // Check capture on safe rosette
        if (rules.IsSharedLane(to))
        {
            int opponentPos = rules.GetOpponentCapturePosition(to);
            if (State.IsOccupiedBy(player.Opponent(), opponentPos)
                && rules.SafeRosettes && rules.IsRosette(to))
                return false; // can't capture on safe rosette
        }

        return true;
    }

    public MoveOutcome ExecuteMove(Move move)
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before executing a move.");
        if (State.IsGameOver)
            throw new InvalidOperationException("Game is over.");

        var validMoves = GetValidMoves();
        if (!validMoves.Any(m => m.From == move.From && m.To == move.To && m.Player == move.Player))
            throw new InvalidOperationException($"Invalid move: {move}");

        var player = move.Player;
        var rules = State.Rules;
        bool captured = false;
        bool borneOff = false;
        int capturedPieceIndex = -1;

        // Move piece(s)
        var pieces = State.GetPiecesMutable(player);
        if (rules.AllowStacking)
        {
            // Move ALL pieces at the from position (stack moves as one)
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i].Position == move.From)
                    pieces[i] = pieces[i] with { Position = move.To };
            }
        }
        else
        {
            // Use move.PieceIndex for direct access
            pieces[move.PieceIndex] = pieces[move.PieceIndex] with { Position = move.To };
        }

        // Check capture using CaptureMap
        if (move.To < rules.PathLength && rules.IsSharedLane(move.To))
        {
            int opponentCapturePos = rules.GetOpponentCapturePosition(move.To);
            var opponentPieces = State.GetPiecesMutable(player.Opponent());

            if (rules.AllowStacking)
            {
                // Capture ALL opponent pieces at the mapped position
                for (int i = 0; i < opponentPieces.Length; i++)
                {
                    if (opponentPieces[i].Position == opponentCapturePos)
                    {
                        if (capturedPieceIndex < 0) capturedPieceIndex = i;
                        opponentPieces[i] = opponentPieces[i] with { Position = -1 };
                        captured = true;
                    }
                }
            }
            else
            {
                for (int i = 0; i < opponentPieces.Length; i++)
                {
                    if (opponentPieces[i].Position == opponentCapturePos)
                    {
                        capturedPieceIndex = i;
                        opponentPieces[i] = opponentPieces[i] with { Position = -1 };
                        captured = true;
                        break;
                    }
                }
            }
        }

        // Check borne off
        if (move.To == rules.PathLength)
        {
            borneOff = true;
            if (State.PiecesBorneOff(player) == rules.PiecesPerPlayer)
            {
                State.Winner = player;
                _hasRolled = false;
                return new MoveOutcome(MoveResult.Win);
            }
        }

        // Determine extra turn
        bool rosetteExtra = move.To < rules.PathLength
            && rules.IsRosette(move.To)
            && rules.RosetteExtraRoll;
        bool captureExtra = captured && rules.CaptureExtraRoll;
        bool extraTurn = rosetteExtra || captureExtra;

        if (!extraTurn)
        {
            State.CurrentPlayer = player.Opponent();
        }

        _hasRolled = false;

        var result = (captured, borneOff, extraTurn) switch
        {
            (true, false, true) => MoveResult.CapturedAndExtraTurn,
            (true, false, false) => MoveResult.Captured,
            (false, true, true) => MoveResult.BorneOffAndExtraTurn,
            (false, true, false) => MoveResult.BorneOff,
            (false, false, true) => MoveResult.ExtraTurn,
            _ => MoveResult.Moved
        };

        return new MoveOutcome(result, capturedPieceIndex);
    }

    public void ForfeitTurn()
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before forfeiting.");
        if (State.IsGameOver)
            throw new InvalidOperationException("Game is over.");

        var validMoves = GetValidMoves();

        if (validMoves.Count > 0)
        {
            if (!State.Rules.AllowVoluntarySkip)
                throw new InvalidOperationException("Cannot forfeit when valid moves exist.");

            // With voluntary skip, only allow if no forward moves exist
            bool hasForwardMoves = validMoves.Any(m => m.To > m.From);
            if (hasForwardMoves)
                throw new InvalidOperationException("Cannot forfeit when forward moves exist.");
        }

        State.CurrentPlayer = State.CurrentPlayer.Opponent();
        _hasRolled = false;
    }
}
