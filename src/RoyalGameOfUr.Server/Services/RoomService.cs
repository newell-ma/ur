using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Services;

public sealed class RoomService : IRoomService
{
    private readonly RoomManager _roomManager;
    private readonly IGameBroadcaster _broadcaster;
    private readonly ILogger<RoomService> _logger;
    private readonly ConcurrentDictionary<string, string> _connectionToRoom = new();
    private readonly ConcurrentDictionary<string, string> _tokenToRoom = new();

    private const int MaxNameLength = 20;

    public RoomService(RoomManager roomManager, IGameBroadcaster broadcaster, ILogger<RoomService> logger)
    {
        _roomManager = roomManager;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    private static string? ValidatePlayerName(ref string name)
    {
        name = name.Trim();
        if (name.Length == 0) return "Player name is required";
        if (name.Length > MaxNameLength) return $"Player name must be {MaxNameLength} characters or fewer";
        return null;
    }

    public CreateRoomResult CreateRoom(string rulesName, string playerName, string connectionId)
    {
        var error = ValidatePlayerName(ref playerName);
        if (error is not null)
            return new CreateRoomResult(false, error, "", "", "");

        var room = _roomManager.CreateRoom(rulesName, playerName, connectionId);
        _connectionToRoom[connectionId] = room.Code;
        _tokenToRoom[room.Player1Token!] = room.Code;
        return new CreateRoomResult(true, "", room.Code, room.RulesName, room.Player1Token!);
    }

    public async Task<JoinRoomResult> JoinRoom(string code, string playerName, string connectionId)
    {
        var error = ValidatePlayerName(ref playerName);
        if (error is not null)
            return new JoinRoomResult(false, error, code, "", "", "");

        var room = _roomManager.GetRoom(code);
        if (room is null)
            return new JoinRoomResult(false, "Room not found", code, "", "", "");

        if (!room.TryJoin(playerName, connectionId))
            return new JoinRoomResult(false, "Room is full or game already started", code, room.RulesName, "", "");

        _connectionToRoom[connectionId] = room.Code;
        _tokenToRoom[room.Player2Token!] = room.Code;

        // Notify the host that the guest has joined
        if (room.Player1 is not null)
            await _broadcaster.SendToPlayer(room.Player1.ConnectionId, "ReceiveOpponentJoined", playerName);

        return new JoinRoomResult(true, "", room.Code, room.RulesName, room.Player1?.Name ?? "", room.Player2Token!);
    }

    public async Task<bool> TryStartGame(string code, string hostConnectionId)
    {
        var room = _roomManager.GetRoom(code);
        if (room is null) return false;
        if (room.Player1?.ConnectionId != hostConnectionId) return false;
        if (room.Player2 is null) return false;
        if (room.IsStarted) return false;

        // Broadcast game starting before the game loop begins to guarantee ordering
        await _broadcaster.BroadcastGameStarting(room.GroupName,
            room.Player1.Name, room.Player2.Name, room.RulesName);

        room.OnGameCompleted = OnRoomGameCompleted;
        room.Start(_broadcaster);
        return true;
    }

    public bool TrySubmitMove(string code, string connectionId, Move move)
    {
        var room = _roomManager.GetRoom(code);
        if (room is null) return false;

        var side = room.GetPlayerSide(connectionId);
        if (side is null) return false;

        var player = room.GetSignalRPlayer(side.Value);
        if (player is null) return false;

        return player.TrySubmitMove(move);
    }

    public bool TrySubmitSkipDecision(string code, string connectionId, bool skip)
    {
        var room = _roomManager.GetRoom(code);
        if (room is null) return false;

        var side = room.GetPlayerSide(connectionId);
        if (side is null) return false;

        var player = room.GetSignalRPlayer(side.Value);
        if (player is null) return false;

        return player.TrySubmitSkipDecision(skip);
    }

    public GameRoom? GetRoom(string code) => _roomManager.GetRoom(code);

    public async Task HandleDisconnect(string connectionId)
    {
        if (!_connectionToRoom.TryRemove(connectionId, out var roomCode))
            return;

        var room = _roomManager.GetRoom(roomCode);
        if (room is null) return;

        // If game hasn't started (lobby phase), do immediate teardown
        if (!room.IsStarted)
        {
            await TeardownRoom(room, roomCode, connectionId);
            return;
        }

        // Game is in progress — start grace period instead of immediate teardown
        var opponent = GetOpponentConnectionId(room, connectionId);
        if (opponent is not null)
            await _broadcaster.SendToPlayer(opponent, "ReceiveOpponentReconnecting");

        room.OnGracePeriodExpired = OnGracePeriodExpired;
        room.StartGracePeriod(connectionId);
    }

    public async Task HandleLeave(string connectionId)
    {
        if (!_connectionToRoom.TryRemove(connectionId, out var roomCode))
            return;

        var room = _roomManager.GetRoom(roomCode);
        if (room is null) return;

        await TeardownRoom(room, roomCode, connectionId);
    }

    public async Task<RejoinResult> HandleRejoin(string sessionToken, string newConnectionId)
    {
        if (!_tokenToRoom.TryGetValue(sessionToken, out var roomCode))
            return new RejoinResult(false, "Invalid session token", "", "", "", "", "");

        var room = _roomManager.GetRoom(roomCode);
        if (room is null || room.IsFinished)
            return new RejoinResult(false, "Room no longer exists", "", "", "", "", "");

        var match = room.GetPlayerByToken(sessionToken);
        if (match is null)
            return new RejoinResult(false, "Player not found in room", "", "", "", "", "");

        var (player, side) = match.Value;
        var oldConnectionId = player.ConnectionId;

        // Swap the connection ID
        player.ConnectionId = newConnectionId;
        _connectionToRoom.TryRemove(oldConnectionId, out _);
        _connectionToRoom[newConnectionId] = roomCode;

        // Cancel grace period
        room.CancelGracePeriod(oldConnectionId);

        // Notify opponent that player reconnected
        var opponentConnectionId = GetOpponentConnectionId(room, newConnectionId);
        if (opponentConnectionId is not null)
            await _broadcaster.SendToPlayer(opponentConnectionId, "ReceiveOpponentReconnected");

        // Re-send game state to reconnected player
        var p1Name = room.Player1?.Name ?? "";
        var p2Name = room.Player2?.Name ?? "";
        await _broadcaster.SendToPlayer(newConnectionId, "ReceiveGameStarting", p1Name, p2Name, room.RulesName);

        if (room.LastStateDto is not null)
            await _broadcaster.SendToPlayer(newConnectionId, "ReceiveStateChanged", room.LastStateDto);

        // If it's their turn and awaiting input, re-send the request
        var signalRPlayer = room.GetSignalRPlayer(side);
        if (signalRPlayer is not null)
        {
            if (signalRPlayer.IsAwaitingMove && signalRPlayer.PendingMoves.Count > 0)
                await _broadcaster.SendToPlayer(newConnectionId, "ReceiveMoveRequired", signalRPlayer.PendingMoves.ToArray(), 0);
            else if (signalRPlayer.IsAwaitingSkip && signalRPlayer.PendingMoves.Count > 0)
                await _broadcaster.SendToPlayer(newConnectionId, "ReceiveSkipRequired", signalRPlayer.PendingMoves.ToArray(), 0);
        }

        var sideStr = side == Player.One ? "One" : "Two";
        return new RejoinResult(true, "", roomCode, room.RulesName, p1Name, p2Name, sideStr);
    }

    private async Task OnGracePeriodExpired(string roomCode, string connectionId)
    {
        try
        {
            var room = _roomManager.GetRoom(roomCode);
            if (room is null) return;

            room.Stop();

            var opponentConnectionId = GetOpponentConnectionId(room, connectionId);
            if (opponentConnectionId is not null)
            {
                await _broadcaster.SendToPlayer(opponentConnectionId, "ReceiveOpponentDisconnected");
                _connectionToRoom.TryRemove(opponentConnectionId, out _);
            }

            CleanupTokens(room);
            _roomManager.RemoveRoom(roomCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in grace period expiry for room {RoomCode}", roomCode);
        }
    }

    private void OnRoomGameCompleted(string roomCode)
    {
        var room = _roomManager.GetRoom(roomCode);
        _roomManager.RemoveRoom(roomCode);

        if (room is null) return;

        if (room.Player1 is not null)
            _connectionToRoom.TryRemove(room.Player1.ConnectionId, out _);
        if (room.Player2 is not null)
            _connectionToRoom.TryRemove(room.Player2.ConnectionId, out _);
        CleanupTokens(room);
    }

    private void CleanupTokens(GameRoom room)
    {
        if (room.Player1Token is not null)
            _tokenToRoom.TryRemove(room.Player1Token, out _);
        if (room.Player2Token is not null)
            _tokenToRoom.TryRemove(room.Player2Token, out _);
    }

    private async Task TeardownRoom(GameRoom room, string roomCode, string connectionId)
    {
        room.Stop();
        var opponentConnectionId = GetOpponentConnectionId(room, connectionId);
        if (opponentConnectionId is not null)
        {
            await _broadcaster.SendToPlayer(opponentConnectionId, "ReceiveOpponentDisconnected");
            _connectionToRoom.TryRemove(opponentConnectionId, out _);
        }
        CleanupTokens(room);
        _roomManager.RemoveRoom(roomCode);
    }

    private static string? GetOpponentConnectionId(GameRoom room, string disconnectedConnectionId)
    {
        if (room.Player1?.ConnectionId == disconnectedConnectionId)
            return room.Player2?.ConnectionId;
        if (room.Player2?.ConnectionId == disconnectedConnectionId)
            return room.Player1?.ConnectionId;
        return null;
    }
}
