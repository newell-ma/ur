using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Engine.Dtos;

namespace RoyalGameOfUr.Server.Rooms;

public interface IGameBroadcaster
{
    Task BroadcastStateChanged(string groupName, GameStateDto state);
    Task BroadcastDiceRolled(string groupName, Player player, int roll);
    Task BroadcastMoveMade(string groupName, Move move, MoveOutcome outcome);
    Task BroadcastTurnForfeited(string groupName, Player player);
    Task BroadcastGameOver(string groupName, Player winner);
    Task BroadcastError(string groupName, string message);
    Task BroadcastGameStarting(string groupName, string player1Name, string player2Name, string rulesName);
    Task SendToPlayer(string connectionId, string method, params object?[] args);
}
