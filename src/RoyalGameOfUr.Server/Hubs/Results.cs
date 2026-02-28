namespace RoyalGameOfUr.Server.Hubs;

public record CreateRoomResult(string Code, string RulesName);
public record JoinRoomResult(bool Success, string Error, string Code, string RulesName, string HostName);
