namespace RoyalGameOfUr.Engine;

public sealed class GameRunner
{
    private readonly Game _game;
    private readonly IPlayer _playerOne;
    private readonly IPlayer _playerTwo;
    private readonly IGameObserver _observer;

    public GameRunner(Game game, IPlayer playerOne, IPlayer playerTwo, IGameObserver observer)
    {
        _game = game;
        _playerOne = playerOne;
        _playerTwo = playerTwo;
        _observer = observer;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _observer.OnStateChanged(_game.State);

        while (!_game.State.IsGameOver)
        {
            ct.ThrowIfCancellationRequested();

            int roll = _game.Roll();
            _observer.OnDiceRolled(_game.State.CurrentPlayer, roll);

            var validMoves = _game.GetValidMoves();

            if (validMoves.Count == 0)
            {
                _game.ForfeitTurn();
                _observer.OnTurnForfeited(_game.State.CurrentPlayer.Opponent());
                _observer.OnStateChanged(_game.State);
                continue;
            }

            // Check for voluntary skip (only backward moves available)
            if (_game.State.Rules.AllowVoluntarySkip)
            {
                bool hasForwardMoves = validMoves.Any(m => m.To > m.From);
                if (!hasForwardMoves)
                {
                    var currentPlayer = GetPlayer(_game.State.CurrentPlayer);
                    bool shouldSkip;
                    if (currentPlayer is ISkipCapablePlayer skipCapable)
                        shouldSkip = await skipCapable.ShouldSkipAsync(
                            _game.State, validMoves, roll);
                    else
                        shouldSkip = true; // default: auto-skip

                    if (shouldSkip)
                    {
                        _game.ForfeitTurn();
                        _observer.OnTurnForfeited(_game.State.CurrentPlayer.Opponent());
                        _observer.OnStateChanged(_game.State);
                        continue;
                    }
                }
            }

            var playerImpl = GetPlayer(_game.State.CurrentPlayer);
            var chosenMove = await playerImpl.ChooseMoveAsync(_game.State, validMoves, roll);
            var result = _game.ExecuteMove(chosenMove);

            _observer.OnMoveMade(chosenMove, result);
            _observer.OnStateChanged(_game.State);

            if (result == MoveResult.Win)
            {
                _observer.OnGameOver(_game.State.Winner!.Value);
            }
        }
    }

    private IPlayer GetPlayer(Player player) =>
        player == Player.One ? _playerOne : _playerTwo;
}
