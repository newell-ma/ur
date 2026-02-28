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

        return move switch
        {
            // Backward moves — penalize
            { To: var to, From: var from } when to < from
                => -100 + to,

            // Bear off — highest priority
            { To: var to } when to == rules.PathLength
                => 1000,

            // Land on rosette
            { To: var to } when rules.IsRosette(to)
                => (rules.RosetteExtraRoll, rules.SafeRosettes) switch
                {
                    (true, true) => 800,
                    (true, false) => 700,
                    (false, true) => 400,
                    (false, false) => 300,
                },

            // Capture opponent — use CaptureMap
            { To: var to, Player: var player } when rules.IsSharedLane(to)
                && state.IsOccupiedBy(player.Opponent(), rules.GetOpponentCapturePosition(to))
                => rules.CaptureExtraRoll ? 750 : 600,

            // Advance furthest piece — prefer pieces closer to bear-off
            { From: >= 0 } => 200 + move.To,

            // Enter from start — get pieces on the board
            _ => 100
        };
    }
}
