namespace RoyalGameOfUr.Server.Hubs;

public record CreateRoomResult(string Code, string RulesName, string SessionToken);
public record JoinRoomResult(bool Success, string Error, string Code, string RulesName, string HostName, string SessionToken);
public record RejoinResult(bool Success, string Error, string Code, string RulesName, string Player1Name, string Player2Name, string PlayerSide);
