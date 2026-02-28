namespace RoyalGameOfUr.Engine;

public interface IPlayer
{
    string Name { get; }
    Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll);
}
