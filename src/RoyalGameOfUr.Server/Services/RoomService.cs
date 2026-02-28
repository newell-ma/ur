using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Services;

public sealed class RoomService : IRoomService
{
    private readonly RoomManager _roomManager;
    private readonly IGameBroadcaster _broadcaster;

    public RoomService(RoomManager roomManager, IGameBroadcaster broadcaster)
    {
        _roomManager = roomManager;
        _broadcaster = broadcaster;
    }

    public CreateRoomResult CreateRoom(string rulesName, string playerName, string connectionId)
    {
        var room = _roomManager.CreateRoom(rulesName, playerName, connectionId);
        return new CreateRoomResult(room.Code, room.RulesName);
    }

    public async Task<JoinRoomResult> JoinRoom(string code, string playerName, string connectionId)
    {
        var room = _roomManager.GetRoom(code);
        if (room is null)
            return new JoinRoomResult(false, "Room not found", code, "", "");

        if (!room.TryJoin(playerName, connectionId))
            return new JoinRoomResult(false, "Room is full or game already started", code, room.RulesName, "");

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
}
