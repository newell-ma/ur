using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Web.Models;

public sealed class BoardCell
{
    public int Col { get; init; }
    public int Row { get; init; }
    public bool IsRosette { get; init; }
    public string Zone { get; init; } = ""; // "private1", "shared", "private2", "exit1", "exit2", "cross1", "cross2"

    /// <summary>
    /// Maps Player -> board position for this cell. Null if cell doesn't exist for that player.
    /// </summary>
    public Dictionary<Player, int> Positions { get; init; } = new();
}
