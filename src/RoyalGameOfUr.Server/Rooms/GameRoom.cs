using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;

namespace RoyalGameOfUr.Server.Rooms;

public sealed class GameRoom : IGameObserver
{
    private CancellationTokenSource? _cts;
    private IGameBroadcaster? _broadcaster;

    public string Code { get; }
    public string RulesName { get; }
    public SignalRPlayer? Player1 { get; }
    public SignalRPlayer? Player2 { get; private set; }
    public bool IsStarted { get; private set; }
    public bool IsFinished { get; private set; }

    public GameRoom(string code, string rulesName, string hostName, string hostConnectionId)
    {
        Code = code;
        RulesName = rulesName;
        Player1 = new SignalRPlayer(hostName, hostConnectionId);
    }

    public string GroupName => $"room-{Code}";

    public bool TryJoin(string guestName, string connectionId)
    {
        if (Player2 is not null || IsStarted) return false;
        Player2 = new SignalRPlayer(guestName, connectionId);
        return true;
    }

    public Player? GetPlayerSide(string connectionId)
    {
        if (Player1?.ConnectionId == connectionId) return Player.One;
        if (Player2?.ConnectionId == connectionId) return Player.Two;
        return null;
    }

    public SignalRPlayer? GetSignalRPlayer(Player player) =>
        player == Player.One ? Player1 : Player2;

    public void Start(IGameBroadcaster broadcaster)
    {
        if (IsStarted || Player1 is null || Player2 is null) return;
        IsStarted = true;

        _broadcaster = broadcaster;
        _cts = new CancellationTokenSource();

        // Wire up move/skip callbacks to send SignalR messages to the active player
        Player1.OnMoveRequired = (moves, roll) =>
            _broadcaster.SendToPlayer(Player1.ConnectionId, "ReceiveMoveRequired", moves.ToArray(), roll);
        Player1.OnSkipRequired = (moves, roll) =>
            _broadcaster.SendToPlayer(Player1.ConnectionId, "ReceiveSkipRequired", moves.ToArray(), roll);

        Player2.OnMoveRequired = (moves, roll) =>
            _broadcaster.SendToPlayer(Player2.ConnectionId, "ReceiveMoveRequired", moves.ToArray(), roll);
        Player2.OnSkipRequired = (moves, roll) =>
            _broadcaster.SendToPlayer(Player2.ConnectionId, "ReceiveSkipRequired", moves.ToArray(), roll);

        var rules = GameStateMapper.ResolveRules(RulesName);
        var dice = new Dice(null, rules.DiceCount);
        var game = new Game(dice, rules);
        var runner = new GameRunner(game, Player1, Player2, this);

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
                await _broadcaster.BroadcastError(GroupName, ex.Message);
            }
            finally
            {
                IsFinished = true;
            }
        }, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        Player1?.Cancel();
        Player2?.Cancel();
    }

    // IGameObserver — called from game loop background thread

    async Task IGameObserver.OnStateChangedAsync(GameState state)
    {
        if (_broadcaster is null) return;
        var dto = GameStateMapper.ToDto(state);
        await _broadcaster.BroadcastStateChanged(GroupName, dto);
    }

    async Task IGameObserver.OnDiceRolledAsync(Player player, int roll)
    {
        if (_broadcaster is null) return;
        await _broadcaster.BroadcastDiceRolled(GroupName, player, roll);
    }

    async Task IGameObserver.OnMoveMadeAsync(Move move, MoveOutcome outcome)
    {
        if (_broadcaster is null) return;
        await _broadcaster.BroadcastMoveMade(GroupName, move, outcome);
    }

    async Task IGameObserver.OnTurnForfeitedAsync(Player player)
    {
        if (_broadcaster is null) return;
        await _broadcaster.BroadcastTurnForfeited(GroupName, player);
    }

    async Task IGameObserver.OnGameOverAsync(Player winner)
    {
        if (_broadcaster is null) return;
        await _broadcaster.BroadcastGameOver(GroupName, winner);
        IsFinished = true;
    }
}
