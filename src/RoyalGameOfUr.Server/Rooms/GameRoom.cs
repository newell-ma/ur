using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;

namespace RoyalGameOfUr.Server.Rooms;

public sealed class GameRoom : IGameObserver
{
    private readonly object _joinLock = new();
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _cts;
    private IGameBroadcaster? _broadcaster;
    private ITimer? _graceTimer;
    private string? _disconnectedConnectionId;

    public string Code { get; }
    public string RulesName { get; }
    public SignalRPlayer? Player1 { get; }
    public SignalRPlayer? Player2 { get; private set; }
    public bool IsStarted { get; private set; }
    public bool IsFinished { get; private set; }
    public Action<string>? OnGameCompleted { get; set; }

    public string? Player1Token { get; private set; }
    public string? Player2Token { get; private set; }
    public GameStateDto? LastStateDto { get; private set; }
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(30);
    public Action<string, string>? OnGracePeriodExpired { get; set; }

    public GameRoom(string code, string rulesName, string hostName, string hostConnectionId, TimeProvider? timeProvider = null)
    {
        Code = code;
        RulesName = rulesName;
        _timeProvider = timeProvider ?? TimeProvider.System;
        Player1 = new SignalRPlayer(hostName, hostConnectionId, _timeProvider);
        Player1Token = Guid.NewGuid().ToString("N");
    }

    public string GroupName => $"room-{Code}";

    public bool TryJoin(string guestName, string connectionId)
    {
        lock (_joinLock)
        {
            if (Player2 is not null || IsStarted) return false;
            Player2 = new SignalRPlayer(guestName, connectionId, _timeProvider);
            Player2Token = Guid.NewGuid().ToString("N");
            return true;
        }
    }

    public Player? GetPlayerSide(string connectionId)
    {
        if (Player1?.ConnectionId == connectionId) return Player.One;
        if (Player2?.ConnectionId == connectionId) return Player.Two;
        return null;
    }

    public SignalRPlayer? GetSignalRPlayer(Player player) =>
        player == Player.One ? Player1 : Player2;

    public (SignalRPlayer Player, Player Side)? GetPlayerByToken(string token)
    {
        if (Player1Token == token && Player1 is not null)
            return (Player1, Player.One);
        if (Player2Token == token && Player2 is not null)
            return (Player2, Player.Two);
        return null;
    }

    public void StartGracePeriod(string connectionId)
    {
        _disconnectedConnectionId = connectionId;
        _graceTimer?.Dispose();
        var roomCode = Code;
        var connId = connectionId;

        _graceTimer = _timeProvider.CreateTimer(_ =>
        {
            OnGracePeriodExpired?.Invoke(roomCode, connId);
        }, null, GracePeriod, Timeout.InfiniteTimeSpan);
    }

    public bool CancelGracePeriod(string connectionId)
    {
        if (_graceTimer is null || _disconnectedConnectionId != connectionId)
            return false;

        _graceTimer.Dispose();
        _graceTimer = null;
        _disconnectedConnectionId = null;
        return true;
    }

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

        // Wire timeout notifications — when a player is slow, notify the opponent
        Player1.OnMoveTimedOut = () =>
            _broadcaster.SendToPlayer(Player2.ConnectionId, "ReceiveOpponentSlow", Player1.Name);
        Player2.OnMoveTimedOut = () =>
            _broadcaster.SendToPlayer(Player1.ConnectionId, "ReceiveOpponentSlow", Player2.Name);

        var rules = GameStateMapper.ResolveRules(RulesName);
        var dice = new Dice(null, rules.DiceCount);
        var game = new Game(dice, rules);
        var runner = new GameRunner(game, Player1, Player2, this);
        var cts = _cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await runner.RunAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Game was stopped (disconnect)
            }
            catch (Exception ex)
            {
                if (_broadcaster is not null)
                    await _broadcaster.BroadcastError(GroupName, ex.Message);
            }
            finally
            {
                IsFinished = true;
                OnGameCompleted?.Invoke(Code);
            }
        }, CancellationToken.None);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _graceTimer?.Dispose();
        _graceTimer = null;
        Player1?.Cancel();
        Player2?.Cancel();
    }

    // IGameObserver — called from game loop background thread

    async Task IGameObserver.OnStateChangedAsync(GameState state)
    {
        if (_broadcaster is null) return;
        var dto = GameStateMapper.ToDto(state);
        LastStateDto = dto;
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
