using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Web.Services;

public sealed class GameService : IGameObserver
{
    private CancellationTokenSource? _cts;
    private BlazorPlayer? _blazorPlayer1;
    private BlazorPlayer? _blazorPlayer2;

    public GameState? State { get; private set; }
    public GameRules Rules { get; private set; } = GameRules.Finkel;
    public int LastRollDisplay { get; private set; }
    public int EffectiveRollDisplay { get; private set; }
    public IReadOnlyList<Move> ValidMoves { get; private set; } = [];
    public bool IsAwaitingMove => ActiveHumanPlayer?.IsAwaitingMove == true;
    public bool IsAwaitingSkip => ActiveHumanPlayer?.IsAwaitingSkip == true;
    public Player? Winner { get; private set; }
    public bool IsRunning { get; private set; }
    public string? StatusMessage { get; private set; }
    public Move? LastMove { get; private set; }
    public MoveOutcome? LastOutcome { get; private set; }
    public string Player1Name { get; private set; } = "Player 1";
    public string Player2Name { get; private set; } = "Player 2";
    public PlayerType Player1Type { get; private set; } = PlayerType.Human;
    public PlayerType Player2Type { get; private set; } = PlayerType.Computer;

    // Dice display
    public bool DiceRolled { get; private set; }
    public int[]? IndividualDice { get; private set; }

    // Events for UI notification
    public event Func<Task>? OnStateChanged;
    public event Func<Task>? OnDiceRolledEvent;
    public event Func<Task>? OnMoveRequested;
    public event Func<Task>? OnSkipRequested;
    public event Func<Task>? OnGameOverEvent;

    private BlazorPlayer? ActiveHumanPlayer
    {
        get
        {
            if (State is null) return null;
            return State.CurrentPlayer == Player.One ? _blazorPlayer1 : _blazorPlayer2;
        }
    }

    public async Task StartGameAsync(
        GameRules rules,
        PlayerType p1Type, string p1Name,
        PlayerType p2Type, string p2Name)
    {
        StopGame();

        Rules = rules;
        Player1Name = p1Name;
        Player2Name = p2Name;
        Player1Type = p1Type;
        Player2Type = p2Type;
        Winner = null;
        StatusMessage = null;
        LastMove = null;
        LastOutcome = null;
        DiceRolled = false;
        IndividualDice = null;
        ValidMoves = [];

        _cts = new CancellationTokenSource();

        var dice = new Dice(null, rules.DiceCount);
        var game = new Game(dice, rules);

        IPlayer player1;
        IPlayer player2;

        if (p1Type == PlayerType.Human)
        {
            _blazorPlayer1 = new BlazorPlayer(p1Name);
            _blazorPlayer1.OnMoveRequested += HandleMoveRequested;
            _blazorPlayer1.OnSkipRequested += HandleSkipRequested;
            player1 = _blazorPlayer1;
        }
        else
        {
            _blazorPlayer1 = null;
            player1 = new GreedyAiPlayer(p1Name, TimeSpan.FromMilliseconds(800));
        }

        if (p2Type == PlayerType.Human)
        {
            _blazorPlayer2 = new BlazorPlayer(p2Name);
            _blazorPlayer2.OnMoveRequested += HandleMoveRequested;
            _blazorPlayer2.OnSkipRequested += HandleSkipRequested;
            player2 = _blazorPlayer2;
        }
        else
        {
            _blazorPlayer2 = null;
            player2 = new GreedyAiPlayer(p2Name, TimeSpan.FromMilliseconds(800));
        }

        var runner = new GameRunner(game, player1, player2, this);
        IsRunning = true;

        // Fire initial state
        if (OnStateChanged is not null)
            await OnStateChanged.Invoke();

        // Run game loop on background thread
        _ = Task.Run(async () =>
        {
            try
            {
                await runner.RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Game was stopped
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                if (OnStateChanged is not null)
                    await OnStateChanged.Invoke();
            }
            finally
            {
                IsRunning = false;
            }
        }, _cts.Token);
    }

    public void StopGame()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _blazorPlayer1?.Cancel();
        _blazorPlayer2?.Cancel();
        _blazorPlayer1 = null;
        _blazorPlayer2 = null;
        IsRunning = false;
    }

    public void SubmitMove(Move move)
    {
        ActiveHumanPlayer?.SubmitMove(move);
    }

    public void SubmitSkipDecision(bool skip)
    {
        ActiveHumanPlayer?.SubmitSkipDecision(skip);
    }

    // Generate random individual dice results that sum to the given total
    private int[] GenerateIndividualDice(int total, int count)
    {
        // Each die is 0 or 1 (50/50 binary). Generate a random distribution that sums to total.
        var rng = Random.Shared;
        var results = new int[count];
        var onesNeeded = total;

        // Randomly place 'total' ones among 'count' dice
        var indices = Enumerable.Range(0, count).OrderBy(_ => rng.Next()).ToArray();
        for (int i = 0; i < Math.Min(onesNeeded, count); i++)
            results[indices[i]] = 1;

        return results;
    }

    // IGameObserver implementation
    async Task IGameObserver.OnStateChangedAsync(GameState state)
    {
        State = state;
        if (OnStateChanged is not null)
            await OnStateChanged.Invoke();
    }

    async Task IGameObserver.OnDiceRolledAsync(Player player, int roll)
    {
        LastRollDisplay = roll;
        EffectiveRollDisplay = State?.EffectiveRoll ?? roll;
        DiceRolled = true;
        IndividualDice = GenerateIndividualDice(roll, Rules.DiceCount);
        var playerName = player == Player.One ? Player1Name : Player2Name;
        StatusMessage = EffectiveRollDisplay != roll
            ? $"{playerName} rolled {roll} (effective: {EffectiveRollDisplay})"
            : $"{playerName} rolled {roll}";
        if (OnDiceRolledEvent is not null)
            await OnDiceRolledEvent.Invoke();
    }

    Task IGameObserver.OnMoveMadeAsync(Move move, MoveOutcome outcome)
    {
        LastMove = move;
        LastOutcome = outcome;

        var playerName = move.Player == Player.One ? Player1Name : Player2Name;
        StatusMessage = outcome.Result switch
        {
            MoveResult.Win => $"{playerName} wins!",
            MoveResult.BorneOff => $"{playerName} bore off a piece",
            MoveResult.BorneOffAndExtraTurn => $"{playerName} bore off a piece - extra turn!",
            MoveResult.Captured => $"{playerName} captured an opponent's piece",
            MoveResult.CapturedAndExtraTurn => $"{playerName} captured - extra turn!",
            MoveResult.ExtraTurn => $"{playerName} landed on a rosette - extra turn!",
            _ => $"{playerName} moved"
        };
        DiceRolled = false;
        ValidMoves = [];
        return Task.CompletedTask;
    }

    async Task IGameObserver.OnTurnForfeitedAsync(Player player)
    {
        var playerName = player == Player.One ? Player1Name : Player2Name;
        StatusMessage = $"{playerName} has no valid moves - turn forfeited";
        DiceRolled = false;
        ValidMoves = [];
        if (OnStateChanged is not null)
            await OnStateChanged.Invoke();
    }

    async Task IGameObserver.OnGameOverAsync(Player winner)
    {
        Winner = winner;
        var playerName = winner == Player.One ? Player1Name : Player2Name;
        StatusMessage = $"{playerName} wins the game!";
        if (OnGameOverEvent is not null)
            await OnGameOverEvent.Invoke();
    }

    private async Task HandleMoveRequested()
    {
        var player = ActiveHumanPlayer;
        if (player is not null)
        {
            ValidMoves = player.PendingMoves;
        }
        if (OnMoveRequested is not null)
            await OnMoveRequested.Invoke();
    }

    private async Task HandleSkipRequested()
    {
        if (OnSkipRequested is not null)
            await OnSkipRequested.Invoke();
    }
}
