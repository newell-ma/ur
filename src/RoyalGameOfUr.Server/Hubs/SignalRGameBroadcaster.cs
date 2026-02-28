using Microsoft.AspNetCore.SignalR;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Hubs;

public sealed class SignalRGameBroadcaster : IGameBroadcaster
{
    private readonly IHubContext<GameHub> _hubContext;

    public SignalRGameBroadcaster(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastStateChanged(string groupName, GameStateDto state) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveStateChanged", [state]);

    public Task BroadcastDiceRolled(string groupName, Player player, int roll) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveDiceRolled", [player, roll]);

    public Task BroadcastMoveMade(string groupName, Move move, MoveOutcome outcome) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveMoveMade", [move, outcome]);

    public Task BroadcastTurnForfeited(string groupName, Player player) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveTurnForfeited", [player]);

    public Task BroadcastGameOver(string groupName, Player winner) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveGameOver", [winner]);

    public Task BroadcastError(string groupName, string message) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveError", [message]);

    public Task BroadcastGameStarting(string groupName, string player1Name, string player2Name, string rulesName) =>
        _hubContext.Clients.Group(groupName).SendCoreAsync("ReceiveGameStarting", [player1Name, player2Name, rulesName]);

    public Task SendToPlayer(string connectionId, string method, params object?[] args) =>
        _hubContext.Clients.Client(connectionId).SendCoreAsync(method, args);
}
