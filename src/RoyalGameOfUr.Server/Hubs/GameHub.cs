using Microsoft.AspNetCore.SignalR;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Services;

namespace RoyalGameOfUr.Server.Hubs;

public sealed class GameHub : Hub
{
    private readonly IRoomService _roomService;

    public GameHub(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task<CreateRoomResult> CreateRoom(string rulesName, string playerName)
    {
        var result = _roomService.CreateRoom(rulesName, playerName, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{result.Code}");
        return result;
    }

    public async Task<JoinRoomResult> JoinRoom(string code, string playerName)
    {
        var result = await _roomService.JoinRoom(code, playerName, Context.ConnectionId);
        if (result.Success)
        {
            var room = _roomService.GetRoom(code)!;
            await Groups.AddToGroupAsync(Context.ConnectionId, room.GroupName);
        }
        return result;
    }

    public async Task StartGame(string code)
    {
        await _roomService.TryStartGame(code, Context.ConnectionId);
    }

    public async Task SubmitMove(string code, Move move)
    {
        if (!_roomService.TrySubmitMove(code, Context.ConnectionId, move))
            await Clients.Caller.SendAsync("ReceiveError", "Invalid move or not your turn");
    }

    public async Task SubmitSkipDecision(string code, bool skip)
    {
        if (!_roomService.TrySubmitSkipDecision(code, Context.ConnectionId, skip))
            await Clients.Caller.SendAsync("ReceiveError", "Not awaiting skip decision");
    }

    public async Task LeaveRoom()
    {
        await _roomService.HandleDisconnect(Context.ConnectionId);
    }

    public async Task<RejoinResult> Rejoin(string sessionToken)
    {
        var result = await _roomService.HandleRejoin(sessionToken, Context.ConnectionId);
        if (result.Success)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{result.Code}");
        }
        return result;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _roomService.HandleDisconnect(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
