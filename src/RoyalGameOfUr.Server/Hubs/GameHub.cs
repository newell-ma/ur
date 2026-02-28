using Microsoft.AspNetCore.SignalR;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Hubs;

public sealed class GameHub : Hub
{
    private readonly RoomManager _rooms;

    public GameHub(RoomManager rooms)
    {
        _rooms = rooms;
    }

    public async Task<object> CreateRoom(string rulesName, string playerName)
    {
        var room = _rooms.CreateRoom(rulesName, playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, room.GroupName);
        return new { room.Code, room.RulesName };
    }

    public async Task<object> JoinRoom(string code, string playerName)
    {
        var room = _rooms.GetRoom(code);
        if (room is null)
            return new { Success = false, Error = "Room not found", Code = code, RulesName = "", HostName = "" };

        if (!room.TryJoin(playerName, Context.ConnectionId))
            return new { Success = false, Error = "Room is full or game already started", Code = code, RulesName = room.RulesName, HostName = "" };

        await Groups.AddToGroupAsync(Context.ConnectionId, room.GroupName);

        // Notify the host that the guest has joined
        if (room.Player1 is not null)
            await Clients.Client(room.Player1.ConnectionId).SendAsync("ReceiveOpponentJoined", playerName);

        return new { Success = true, Error = "", Code = room.Code, RulesName = room.RulesName, HostName = room.Player1?.Name ?? "" };
    }

    public async Task StartGame(string code)
    {
        var room = _rooms.GetRoom(code);
        if (room is null) return;

        // Only the host can start
        if (room.Player1?.ConnectionId != Context.ConnectionId) return;
        if (room.Player2 is null) return;

        await Clients.Group(room.GroupName).SendAsync("ReceiveGameStarting",
            room.Player1.Name, room.Player2.Name, room.RulesName);

        var hubContext = Context.GetHttpContext()!.RequestServices.GetRequiredService<IHubContext<GameHub>>();
        room.Start(hubContext);
    }

    public async Task SubmitMove(string code, Move move)
    {
        var room = _rooms.GetRoom(code);
        if (room is null) return;

        var side = room.GetPlayerSide(Context.ConnectionId);
        if (side is null) return;

        var player = room.GetSignalRPlayer(side.Value);
        if (player is null) return;

        if (!player.TrySubmitMove(move))
            await Clients.Caller.SendAsync("ReceiveError", "Invalid move or not your turn");
    }

    public async Task SubmitSkipDecision(string code, bool skip)
    {
        var room = _rooms.GetRoom(code);
        if (room is null) return;

        var side = room.GetPlayerSide(Context.ConnectionId);
        if (side is null) return;

        var player = room.GetSignalRPlayer(side.Value);
        if (player is null) return;

        if (!player.TrySubmitSkipDecision(skip))
            await Clients.Caller.SendAsync("ReceiveError", "Not awaiting skip decision");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Simple cleanup — no reconnection support for now
        await base.OnDisconnectedAsync(exception);
    }
}
