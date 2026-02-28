namespace RoyalGameOfUr.Engine;

public sealed class GameState
{
    public GameRules Rules { get; }
    public Player CurrentPlayer { get; internal set; }
    public Player? Winner { get; internal set; }
    public int LastRoll { get; internal set; }
    public int EffectiveRoll { get; internal set; }

    private readonly int[] _playerOnePieces;
    private readonly int[] _playerTwoPieces;

    internal GameState(GameRules rules, int[] playerOnePieces, int[] playerTwoPieces, Player currentPlayer)
    {
        Rules = rules;
        _playerOnePieces = playerOnePieces;
        _playerTwoPieces = playerTwoPieces;
        CurrentPlayer = currentPlayer;
        Winner = null;
        LastRoll = -1;
        EffectiveRoll = -1;
    }

    public ReadOnlySpan<int> GetPieces(Player player) =>
        player == Player.One ? _playerOnePieces : _playerTwoPieces;

    internal Span<int> GetPiecesMutable(Player player) =>
        player == Player.One ? _playerOnePieces : _playerTwoPieces;

    public int PiecesAtStart(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (int p in pieces)
            if (p == -1) count++;
        return count;
    }

    public int PiecesBorneOff(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (int p in pieces)
            if (p == Rules.PathLength) count++;
        return count;
    }

    public int PiecesOnBoard(Player player)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (int p in pieces)
            if (p >= 0 && p < Rules.PathLength) count++;
        return count;
    }

    public bool IsOccupiedBy(Player player, int position)
    {
        var pieces = GetPieces(player);
        foreach (int p in pieces)
            if (p == position) return true;
        return false;
    }

    public int PieceCountAt(Player player, int position)
    {
        var pieces = GetPieces(player);
        int count = 0;
        foreach (int p in pieces)
            if (p == position) count++;
        return count;
    }

    public bool IsGameOver => Winner.HasValue;
}
