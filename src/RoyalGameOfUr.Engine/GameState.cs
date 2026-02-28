namespace RoyalGameOfUr.Engine;

public sealed class GameState
{
    public GameRules Rules { get; }
    public Player CurrentPlayer { get; internal set; }
    public Player? Winner { get; internal set; }
    public int LastRoll { get; internal set; }
    public int EffectiveRoll { get; internal set; }

    private readonly Piece[] _playerOnePieces;
    private readonly Piece[] _playerTwoPieces;

    internal GameState(GameRules rules, Piece[] playerOnePieces, Piece[] playerTwoPieces, Player currentPlayer)
    {
        Rules = rules;
        _playerOnePieces = playerOnePieces;
        _playerTwoPieces = playerTwoPieces;
        CurrentPlayer = currentPlayer;
        Winner = null;
        LastRoll = -1;
        EffectiveRoll = -1;
    }

    public ReadOnlySpan<Piece> GetPieces(Player player) =>
        player == Player.One ? _playerOnePieces : _playerTwoPieces;

    internal Span<Piece> GetPiecesMutable(Player player) =>
        player == Player.One ? _playerOnePieces : _playerTwoPieces;

    public int PiecesAtStart(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (var p in pieces)
            if (p.Position == -1) count++;
        return count;
    }

    public int PiecesBorneOff(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (var p in pieces)
            if (p.Position == Rules.PathLength) count++;
        return count;
    }

    public int PiecesOnBoard(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (var p in pieces)
            if (p.Position >= 0 && p.Position < Rules.PathLength) count++;
        return count;
    }

    public bool IsOccupiedBy(Player player, int position)
    {
        var pieces = GetPieces(player);
        foreach (var p in pieces)
            if (p.Position == position) return true;
        return false;
    }

    public int PieceCountAt(Player player, int position)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (var p in pieces)
            if (p.Position == position) count++;
        return count;
    }

    public bool IsGameOver => Winner is not null;
}
