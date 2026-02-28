using System.Collections.Concurrent;

namespace RoyalGameOfUr.Server.Rooms;

public sealed class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private static readonly Random _rng = new();

    public GameRoom CreateRoom(string rulesName, string hostName, string hostConnectionId)
    {
        string code;
        do
        {
            code = GenerateCode();
        } while (!_rooms.TryAdd(code, null!)); // reserve the key

        var room = new GameRoom(code, rulesName, hostName, hostConnectionId);
        _rooms[code] = room;
        return room;
    }

    public GameRoom? GetRoom(string code)
    {
        _rooms.TryGetValue(code.ToUpperInvariant(), out var room);
        return room;
    }

    public bool RemoveRoom(string code)
    {
        return _rooms.TryRemove(code.ToUpperInvariant(), out _);
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/O/0/1 for clarity
        Span<char> buf = stackalloc char[4];
        lock (_rng)
        {
            for (int i = 0; i < 4; i++)
                buf[i] = chars[_rng.Next(chars.Length)];
        }
        return new string(buf);
    }
}
