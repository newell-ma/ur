namespace RoyalGameOfUr.Engine;

public sealed class GameRunner
{
    private readonly Game _game;
    private readonly IPlayer _playerOne;
    private readonly IPlayer _playerTwo;

    public event Action<GameState>? OnStateChanged;
    public event Action<Player, int>? OnDiceRolled;
    public event Action<Move, MoveResult>? OnMoveMade;
    public event Action<Player>? OnTurnForfeited;
    public event Action<Player>? OnGameOver;

    public GameRunner(Game game, IPlayer playerOne, IPlayer playerTwo)
    {
        _game = game;
        _playerOne = playerOne;
        _playerTwo = playerTwo;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        OnStateChanged?.Invoke(_game.State);

        while (!_game.State.IsGameOver)
        {
            ct.ThrowIfCancellationRequested();

            int roll = _game.Roll();
            OnDiceRolled?.Invoke(_game.State.CurrentPlayer, roll);

            var validMoves = _game.GetValidMoves();

            if (validMoves.Count == 0)
            {
                _game.ForfeitTurn();
                OnTurnForfeited?.Invoke(_game.State.CurrentPlayer.Opponent());
                OnStateChanged?.Invoke(_game.State);
                continue;
            }

            var currentPlayer = GetPlayer(_game.State.CurrentPlayer);
            var chosenMove = await currentPlayer.ChooseMoveAsync(_game.State, validMoves, roll);
            var result = _game.ExecuteMove(chosenMove);

            OnMoveMade?.Invoke(chosenMove, result);
            OnStateChanged?.Invoke(_game.State);

            if (result == MoveResult.Win)
            {
                OnGameOver?.Invoke(_game.State.Winner!.Value);
            }
        }
    }

    private IPlayer GetPlayer(Player player) =>
        player == Player.One ? _playerOne : _playerTwo;
}
