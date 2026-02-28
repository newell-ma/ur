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
        _hasRolled = true;
        return roll;
    }

    public IReadOnlyList<Move> GetValidMoves()
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before getting moves.");

        int roll = State.LastRoll;
        if (roll == 0)
            return Array.Empty<Move>();

        var player = State.CurrentPlayer;
        var pieces = State.GetPieces(player);
        var rules = State.Rules;
        var moves = new List<Move>();
        var seen = new HashSet<(int from, int to)>();

        foreach (int from in pieces)
        {
            if (from == rules.PathLength) continue; // already borne off

            int to = from + roll;

            if (to > rules.PathLength) continue; // overshoot

            if (to < rules.PathLength && State.IsOccupiedBy(player, to))
                continue; // own piece blocking

            if (to < rules.PathLength && rules.IsSharedLane(to) &&
                State.IsOccupiedBy(player.Opponent(), to) && rules.IsRosette(to))
                continue; // can't capture on rosette

            if (seen.Add((from, to)))
                moves.Add(new Move(player, from, to));
        }

        return moves;
    }

    public MoveResult ExecuteMove(Move move)
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before executing a move.");
        if (State.IsGameOver)
            throw new InvalidOperationException("Game is over.");

        var validMoves = GetValidMoves();
        if (!validMoves.Contains(move))
            throw new InvalidOperationException($"Invalid move: {move}");

        var player = move.Player;
        var rules = State.Rules;
        var pieces = State.GetPiecesMutable(player);
        bool captured = false;
        bool borneOff = false;
        bool rosette = false;

        // Find the piece and move it
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == move.From)
            {
                pieces[i] = move.To;
                break;
            }
        }

        // Check capture
        if (move.To < rules.PathLength && rules.IsSharedLane(move.To))
        {
            var opponentPieces = State.GetPiecesMutable(player.Opponent());
            for (int i = 0; i < opponentPieces.Length; i++)
            {
                if (opponentPieces[i] == move.To)
                {
                    opponentPieces[i] = -1; // send back to start
                    captured = true;
                    break;
                }
            }
        }

        // Check borne off
        if (move.To == rules.PathLength)
        {
            borneOff = true;
            // Check win
            if (State.PiecesBorneOff(player) == rules.PiecesPerPlayer)
            {
                State.Winner = player;
                _hasRolled = false;
                return MoveResult.Win;
            }
        }

        // Check rosette for extra turn
        if (move.To < rules.PathLength && rules.IsRosette(move.To))
        {
            rosette = true;
        }

        if (!rosette)
        {
            State.CurrentPlayer = player.Opponent();
        }

        _hasRolled = false;

        return (captured, borneOff, rosette) switch
        {
            (true, false, true) => MoveResult.CapturedAndExtraTurn,
            (true, false, false) => MoveResult.Captured,
            (false, true, true) => MoveResult.BorneOffAndExtraTurn,
            (false, true, false) => MoveResult.BorneOff,
            (false, false, true) => MoveResult.ExtraTurn,
            _ => MoveResult.Moved
        };
    }

    public void ForfeitTurn()
    {
        if (!_hasRolled)
            throw new InvalidOperationException("Must roll before forfeiting.");
        if (State.IsGameOver)
            throw new InvalidOperationException("Game is over.");

        var validMoves = GetValidMoves();
        if (validMoves.Count > 0)
            throw new InvalidOperationException("Cannot forfeit when valid moves exist.");

        State.CurrentPlayer = State.CurrentPlayer.Opponent();
        _hasRolled = false;
    }
}
