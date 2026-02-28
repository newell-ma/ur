using System.Collections.Concurrent;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Services;

public sealed class RoomService : IRoomService
{
    private readonly RoomManager _roomManager;
    private readonly IGameBroadcaster _broadcaster;
    private readonly ConcurrentDictionary<string, string> _connectionToRoom = new();

    public RoomService(RoomManager roomManager, IGameBroadcaster broadcaster)
    {
        _roomManager = roomManager;
        _broadcaster = broadcaster;
    }

    public CreateRoomResult CreateRoom(string rulesName, string playerName, string connectionId)
    {
        var room = _roomManager.CreateRoom(rulesName, playerName, connectionId);
        _connectionToRoom[connectionId] = room.Code;
        return new CreateRoomResult(room.Code, room.RulesName);
    }

    public async Task<JoinRoomResult> JoinRoom(string code, string playerName, string connectionId)
    {
        var room = _roomManager.GetRoom(code);
        if (room is null)
            return new JoinRoomResult(false, "Room not found", code, "", "");

        if (!room.TryJoin(playerName, connectionId))
            return new JoinRoomResult(false, "Room is full or game already started", code, room.RulesName, "");

        _connectionToRoom[connectionId] = room.Code;

        // Notify the host that the guest has joined
        if (room.Player1 is not null)
            await _broadcaster.SendToPlayer(room.Player1.ConnectionId, "ReceiveOpponentJoined", playerName);

        return new JoinRoomResult(true, "", room.Code, room.RulesName, room.Player1?.Name ?? "");
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

        room.Stop();

        // Find the opponent and notify them
        var opponentConnectionId = GetOpponentConnectionId(room, connectionId);
        if (opponentConnectionId is not null)
        {
            await _broadcaster.SendToPlayer(opponentConnectionId, "ReceiveOpponentDisconnected");
            _connectionToRoom.TryRemove(opponentConnectionId, out _);
        }

        _roomManager.RemoveRoom(roomCode);
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
