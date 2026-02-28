namespace RoyalGameOfUr.Engine;

public sealed class GreedyAiPlayer : IPlayer
{
    public string Name { get; }
    public TimeSpan ThinkingDelay { get; }

    public GreedyAiPlayer(string name, TimeSpan? thinkingDelay = null)
    {
        Name = name;
        ThinkingDelay = thinkingDelay ?? TimeSpan.FromMilliseconds(500);
    }

    public async Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        if (ThinkingDelay > TimeSpan.Zero)
            await Task.Delay(ThinkingDelay);

        var best = validMoves[0];
        int bestScore = ScoreMove(best, state);

        for (int i = 1; i < validMoves.Count; i++)
        {
            int score = ScoreMove(validMoves[i], state);
            if (score > bestScore)
            {
                best = validMoves[i];
                bestScore = score;
            }
        }

        return best;
    }

    private static int ScoreMove(Move move, GameState state)
    {
        var rules = state.Rules;

        // Bear off — highest priority
        if (move.To == rules.PathLength)
            return 1000;

        // Land on rosette — extra turn is very valuable
        if (rules.IsRosette(move.To))
            return 800;

        // Capture opponent — send them back to start
        if (rules.IsSharedLane(move.To) && state.IsOccupiedBy(move.Player.Opponent(), move.To))
            return 600;

        // Advance furthest piece — prefer pieces closer to bear-off
        if (move.From >= 0)
            return 200 + move.To;

        // Enter from start — get pieces on the board
        return 100;
    }
}
