namespace RoyalGameOfUr.Engine;

public sealed class GreedyAiPlayer : ISkipCapablePlayer
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

    public Task<bool> ShouldSkipAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        // AI always skips when only backward moves available
        return Task.FromResult(true);
    }

    private static int ScoreMove(Move move, GameState state)
    {
        var rules = state.Rules;

        // Backward moves — penalize
        if (move.To < move.From)
            return -100 + move.To;

        // Bear off — highest priority
        if (move.To == rules.PathLength)
            return 1000;

        // Land on rosette
        if (rules.IsRosette(move.To))
        {
            int rosetteScore = 800;
            if (!rules.RosetteExtraRoll) rosetteScore = 400;
            if (!rules.SafeRosettes) rosetteScore -= 100;
            return rosetteScore;
        }

        // Capture opponent — use CaptureMap
        if (rules.IsSharedLane(move.To))
        {
            int opponentPos = rules.GetOpponentCapturePosition(move.To);
            if (state.IsOccupiedBy(move.Player.Opponent(), opponentPos))
            {
                int captureScore = 600;
                if (rules.CaptureExtraRoll) captureScore = 750;
                return captureScore;
            }
        }

        // Advance furthest piece — prefer pieces closer to bear-off
        if (move.From >= 0)
            return 200 + move.To;

        // Enter from start — get pieces on the board
        return 100;
    }
}
