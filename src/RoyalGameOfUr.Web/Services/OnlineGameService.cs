using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;

namespace RoyalGameOfUr.Web.Services;

public sealed class OnlineGameService : IAsyncDisposable, IDisposable
{
    private readonly NavigationManager _nav;
    private HubConnection? _hub;

    // Room state
    public string? RoomCode { get; internal set; }
    public string? RulesName { get; internal set; }
    public bool IsHost { get; internal set; }
    public bool OpponentJoined { get; internal set; }
    public string? OpponentName { get; internal set; }
    public string? ErrorMessage { get; internal set; }

    // Game state (mirrors GameService surface)
    public GameState? State { get; internal set; }
    public GameRules? Rules { get; internal set; }
    public int LastRollDisplay { get; internal set; }
    public int EffectiveRollDisplay { get; internal set; }
    public IReadOnlyList<Move> ValidMoves { get; internal set; } = [];
    public bool IsAwaitingMove { get; internal set; }
    public bool IsAwaitingSkip { get; internal set; }
    public Player? Winner { get; internal set; }
    public bool IsRunning { get; internal set; }
    public string? StatusMessage { get; internal set; }
    public Move? LastMove { get; internal set; }
    public MoveOutcome? LastOutcome { get; internal set; }
    public string Player1Name { get; internal set; } = "Player 1";
    public string Player2Name { get; internal set; } = "Player 2";
    public Player? LocalPlayer { get; internal set; }
    public string LocalPlayerName { get; internal set; } = "";
    public bool IsMyTurn => State is not null && !State.IsGameOver && LocalPlayer == State.CurrentPlayer;

    // Dice display
    public bool DiceRolled { get; internal set; }
    public int[]? IndividualDice { get; internal set; }

    // Disconnect / timeout / reconnection state
    public bool OpponentDisconnected { get; internal set; }
    public bool OpponentSlow { get; internal set; }
    public bool OpponentReconnecting { get; internal set; }
    public string? SessionToken { get; internal set; }

    // Events for UI notification
    public event Func<Task>? OnStateChanged;
    public event Func<Task>? OnDiceRolledEvent;
    public event Func<Task>? OnMoveRequested;
    public event Func<Task>? OnSkipRequested;
    public event Func<Task>? OnGameOverEvent;
    public event Func<Task>? OnRoomUpdated;
    public event Func<Task>? OnGameStarting;

    public OnlineGameService(NavigationManager nav)
    {
        _nav = nav;
    }

    public async Task ConnectAsync()
    {
        if (_hub is not null) return;

        _hub = new HubConnectionBuilder()
            .WithUrl(_nav.ToAbsoluteUri("/gamehub"))
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
        await _hub.StartAsync();
    }

    private void RegisterHandlers()
    {
        if (_hub is null) return;

        _hub.On<string, string, string>("ReceiveGameStarting", async (p1Name, p2Name, rulesName) =>
        {
            Player1Name = p1Name;
            Player2Name = p2Name;
            RulesName = rulesName;
            Rules = GameStateMapper.ResolveRules(rulesName);
            IsRunning = true;
            StatusMessage = "Game starting...";
            if (OnGameStarting is not null) await OnGameStarting.Invoke();
        });

        _hub.On<GameStateDto>("ReceiveStateChanged", async (dto) =>
        {
            Rules ??= GameStateMapper.ResolveRules(dto.RulesName);
            State = GameStateMapper.FromDto(dto);
            LastRollDisplay = dto.LastRoll;
            EffectiveRollDisplay = dto.EffectiveRoll;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On<Player, int>("ReceiveDiceRolled", async (player, roll) =>
        {
            LastRollDisplay = roll;
            EffectiveRollDisplay = State?.EffectiveRoll ?? roll;
            DiceRolled = true;
            IndividualDice = GenerateIndividualDice(roll, Rules?.DiceCount ?? 4);
            var playerName = player == Player.One ? Player1Name : Player2Name;
            StatusMessage = EffectiveRollDisplay != roll
                ? $"{playerName} rolled {roll} (effective: {EffectiveRollDisplay})"
                : $"{playerName} rolled {roll}";
            if (OnDiceRolledEvent is not null) await OnDiceRolledEvent.Invoke();
        });

        _hub.On<Move, MoveOutcome>("ReceiveMoveMade", async (move, outcome) =>
        {
            LastMove = move;
            LastOutcome = outcome;
            IsAwaitingMove = false;
            ValidMoves = [];
            OpponentSlow = false;
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
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On<Player>("ReceiveTurnForfeited", async (player) =>
        {
            var playerName = player == Player.One ? Player1Name : Player2Name;
            StatusMessage = $"{playerName} has no valid moves - turn forfeited";
            DiceRolled = false;
            ValidMoves = [];
            IsAwaitingMove = false;
            IsAwaitingSkip = false;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On<Player>("ReceiveGameOver", async (winner) =>
        {
            Winner = winner;
            IsRunning = false;
            IsAwaitingMove = false;
            IsAwaitingSkip = false;
            ValidMoves = [];
            var playerName = winner == Player.One ? Player1Name : Player2Name;
            StatusMessage = $"{playerName} wins the game!";
            if (OnGameOverEvent is not null) await OnGameOverEvent.Invoke();
        });

        _hub.On<Move[], int>("ReceiveMoveRequired", async (validMoves, roll) =>
        {
            ValidMoves = validMoves;
            IsAwaitingMove = true;
            IsAwaitingSkip = false;
            if (OnMoveRequested is not null) await OnMoveRequested.Invoke();
        });

        _hub.On<Move[], int>("ReceiveSkipRequired", async (validMoves, roll) =>
        {
            ValidMoves = validMoves;
            IsAwaitingSkip = true;
            IsAwaitingMove = false;
            if (OnSkipRequested is not null) await OnSkipRequested.Invoke();
        });

        _hub.On<string>("ReceiveOpponentJoined", async (name) =>
        {
            OpponentJoined = true;
            OpponentName = name;
            if (OnRoomUpdated is not null) await OnRoomUpdated.Invoke();
        });

        _hub.On<string>("ReceiveError", async (message) =>
        {
            ErrorMessage = message;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On<string>("ReceiveOpponentSlow", async (_) =>
        {
            OpponentSlow = true;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On("ReceiveOpponentDisconnected", async () =>
        {
            OpponentDisconnected = true;
            OpponentReconnecting = false;
            IsRunning = false;
            IsAwaitingMove = false;
            IsAwaitingSkip = false;
            ValidMoves = [];
            StatusMessage = "Opponent disconnected";
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On("ReceiveOpponentReconnecting", async () =>
        {
            OpponentReconnecting = true;
            OpponentSlow = false;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.On("ReceiveOpponentReconnected", async () =>
        {
            OpponentReconnecting = false;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        });

        _hub.Reconnected += OnReconnected;
    }

    private async Task OnReconnected(string? _)
    {
        if (SessionToken is null || _hub is null) return;
        var result = await _hub.InvokeAsync<RejoinResult>("Rejoin", SessionToken);
        if (!result.Success)
        {
            ErrorMessage = result.Error;
            if (OnStateChanged is not null) await OnStateChanged.Invoke();
        }
    }

    public async Task<(bool Success, string? Error)> CreateRoomAsync(string rulesName, string playerName)
    {
        if (_hub is null) return (false, "Not connected");

        LocalPlayerName = playerName;
        IsHost = true;
        LocalPlayer = Player.One;

        var result = await _hub.InvokeAsync<CreateRoomResult>("CreateRoom", rulesName, playerName);
        RoomCode = result.Code;
        RulesName = result.RulesName;
        Rules = GameStateMapper.ResolveRules(result.RulesName);
        Player1Name = playerName;
        SessionToken = result.SessionToken;
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> JoinRoomAsync(string code, string playerName)
    {
        if (_hub is null) return (false, "Not connected");

        LocalPlayerName = playerName;
        IsHost = false;
        LocalPlayer = Player.Two;

        var result = await _hub.InvokeAsync<JoinRoomResult>("JoinRoom", code, playerName);
        if (!result.Success)
            return (false, result.Error);

        RoomCode = result.Code;
        RulesName = result.RulesName;
        Rules = GameStateMapper.ResolveRules(result.RulesName);
        Player1Name = result.HostName;
        Player2Name = playerName;
        OpponentName = result.HostName;
        OpponentJoined = true;
        SessionToken = result.SessionToken;
        return (true, null);
    }

    public async Task StartGameAsync()
    {
        if (_hub is null || RoomCode is null) return;
        await _hub.SendAsync("StartGame", RoomCode);
    }

    public async Task SubmitMoveAsync(Move move)
    {
        if (_hub is null || RoomCode is null) return;
        IsAwaitingMove = false;
        ValidMoves = [];
        await _hub.SendAsync("SubmitMove", RoomCode, move);
    }

    public async Task SubmitSkipDecisionAsync(bool skip)
    {
        if (_hub is null || RoomCode is null) return;
        IsAwaitingSkip = false;
        await _hub.SendAsync("SubmitSkipDecision", RoomCode, skip);
    }

    public async Task LeaveRoomAsync()
    {
        if (RoomCode is null) return;
        if (_hub is not null)
        {
            try { await _hub.SendAsync("LeaveRoom"); }
            catch (InvalidOperationException) { /* connection already closed */ }
        }
        Reset();
    }

    public async Task LeaveGameAsync()
    {
        if (_hub is not null)
        {
            _hub.Reconnected -= OnReconnected;
            await _hub.StopAsync();
            await _hub.DisposeAsync();
            _hub = null;
        }
        Reset();
    }

    public void Reset()
    {
        RoomCode = null;
        RulesName = null;
        IsHost = false;
        OpponentJoined = false;
        OpponentName = null;
        ErrorMessage = null;
        State = null;
        Rules = null;
        ValidMoves = [];
        IsAwaitingMove = false;
        IsAwaitingSkip = false;
        Winner = null;
        IsRunning = false;
        StatusMessage = null;
        LastMove = null;
        LastOutcome = null;
        DiceRolled = false;
        IndividualDice = null;
        LocalPlayer = null;
        OpponentDisconnected = false;
        OpponentSlow = false;
        OpponentReconnecting = false;
        SessionToken = null;
    }

    private static int[] GenerateIndividualDice(int total, int count)
    {
        var rng = Random.Shared;
        var results = new int[count];
        var indices = Enumerable.Range(0, count).OrderBy(_ => rng.Next()).ToArray();
        for (int i = 0; i < Math.Min(total, count); i++)
            results[indices[i]] = 1;
        return results;
    }

    public void Dispose()
    {
        _hub?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    // DTOs for hub invoke results
    private record CreateRoomResult(string Code, string RulesName, string SessionToken);
    private record JoinRoomResult(bool Success, string Error, string Code, string RulesName, string HostName, string SessionToken);
    private record RejoinResult(bool Success, string Error, string Code, string RulesName, string Player1Name, string Player2Name, string PlayerSide);
}
