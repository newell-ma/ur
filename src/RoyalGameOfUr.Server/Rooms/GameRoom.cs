using Microsoft.AspNetCore.SignalR;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;
using RoyalGameOfUr.Server.Hubs;

namespace RoyalGameOfUr.Server.Rooms;

public sealed class GameRoom : IGameObserver
{
    private CancellationTokenSource? _cts;
    private IHubContext<GameHub>? _hubContext;

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

    public void Start(IHubContext<GameHub> hubContext)
    {
        if (IsStarted || Player1 is null || Player2 is null) return;
        IsStarted = true;

        _hubContext = hubContext;
        _cts = new CancellationTokenSource();

        // Wire up move/skip callbacks to send SignalR messages to the active player
        Player1.OnMoveRequired = (moves, roll) =>
            SendToPlayer(Player1, "ReceiveMoveRequired", moves.ToArray(), roll);
        Player1.OnSkipRequired = (moves, roll) =>
            SendToPlayer(Player1, "ReceiveSkipRequired", moves.ToArray(), roll);

        Player2.OnMoveRequired = (moves, roll) =>
            SendToPlayer(Player2, "ReceiveMoveRequired", moves.ToArray(), roll);
        Player2.OnSkipRequired = (moves, roll) =>
            SendToPlayer(Player2, "ReceiveSkipRequired", moves.ToArray(), roll);

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
                await SendToGroup("ReceiveError", ex.Message);
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
        var dto = GameStateMapper.ToDto(state);
        await SendToGroup("ReceiveStateChanged", dto);
    }

    async Task IGameObserver.OnDiceRolledAsync(Player player, int roll)
    {
        await SendToGroup("ReceiveDiceRolled", player, roll);
    }

    async Task IGameObserver.OnMoveMadeAsync(Move move, MoveOutcome outcome)
    {
        await SendToGroup("ReceiveMoveMade", move, outcome);
    }

    async Task IGameObserver.OnTurnForfeitedAsync(Player player)
    {
        await SendToGroup("ReceiveTurnForfeited", player);
    }

    async Task IGameObserver.OnGameOverAsync(Player winner)
    {
        await SendToGroup("ReceiveGameOver", winner);
        IsFinished = true;
    }

    private Task SendToGroup(string method, params object?[] args)
    {
        if (_hubContext is null) return Task.CompletedTask;
        return _hubContext.Clients.Group(GroupName).SendCoreAsync(method, args);
    }

    private Task SendToPlayer(SignalRPlayer player, string method, params object?[] args)
    {
        if (_hubContext is null) return Task.CompletedTask;
        return _hubContext.Clients.Client(player.ConnectionId).SendCoreAsync(method, args);
    }
}
