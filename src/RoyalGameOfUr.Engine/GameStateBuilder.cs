namespace RoyalGameOfUr.Engine;

public sealed class GameStateBuilder
{
    private readonly GameRules _rules;
    private readonly List<(int Id, int Position)> _playerOnePieces = new();
    private readonly List<(int Id, int Position)> _playerTwoPieces = new();
    private Player _currentPlayer = Player.One;

    public GameStateBuilder(GameRules rules)
    {
        _rules = rules;
    }

    public GameStateBuilder WithPiece(Player player, int position)
    {
        var list = player == Player.One ? _playerOnePieces : _playerTwoPieces;
        list.Add((list.Count, position));
        return this;
    }

    public GameStateBuilder WithPiece(Player player, int id, int position)
    {
        var list = player == Player.One ? _playerOnePieces : _playerTwoPieces;
        list.Add((id, position));
        return this;
    }

    public GameStateBuilder WithCurrentPlayer(Player player)
    {
        _currentPlayer = player;
        return this;
    }

    public GameState Build()
    {
        // Fill remaining slots with -1 (at start)
        Piece[] p1 = BuildPieceArray(_playerOnePieces);
        Piece[] p2 = BuildPieceArray(_playerTwoPieces);
        return new GameState(_rules, p1, p2, _currentPlayer);
    }

    private Piece[] BuildPieceArray(List<(int Id, int Position)> explicitPieces)
    {
        var pieces = new Piece[_rules.PiecesPerPlayer];
        for (int i = 0; i < pieces.Length; i++)
            pieces[i] = new Piece(i, -1);
        for (int i = 0; i < explicitPieces.Count && i < pieces.Length; i++)
            pieces[i] = new Piece(explicitPieces[i].Id, explicitPieces[i].Position);
        return pieces;
    }
}
