namespace RoyalGameOfUr.Engine;

public interface ISkipCapablePlayer : IPlayer
{
    Task<bool> ShouldSkipAsync(GameState state, IReadOnlyList<Move> validMoves, int roll);
}
