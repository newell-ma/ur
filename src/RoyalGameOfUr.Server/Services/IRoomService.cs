using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Hubs;
using RoyalGameOfUr.Server.Rooms;

namespace RoyalGameOfUr.Server.Services;

public interface IRoomService
{
    CreateRoomResult CreateRoom(string rulesName, string playerName, string connectionId);
    Task<JoinRoomResult> JoinRoom(string code, string playerName, string connectionId);
    Task<bool> TryStartGame(string code, string hostConnectionId);
    bool TrySubmitMove(string code, string connectionId, Move move);
    bool TrySubmitSkipDecision(string code, string connectionId, bool skip);
    GameRoom? GetRoom(string code);
    Task HandleDisconnect(string connectionId);
    Task HandleLeave(string connectionId);
    Task<RejoinResult> HandleRejoin(string sessionToken, string newConnectionId);
}
