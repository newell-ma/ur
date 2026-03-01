using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Web.Services;

public sealed class BlazorPlayer : ISkipCapablePlayer
{
    private TaskCompletionSource<Move>? _moveTcs;
    private TaskCompletionSource<bool>? _skipTcs;

    public string Name { get; }
    public Func<Task>? OnMoveRequested { get; set; }
    public Func<Task>? OnSkipRequested { get; set; }

    public IReadOnlyList<Move> PendingMoves { get; private set; } = [];
    public int PendingRoll { get; private set; }
    public bool IsAwaitingMove => _moveTcs is not null;
    public bool IsAwaitingSkip => _skipTcs is not null;

    public BlazorPlayer(string name)
    {
        Name = name;
    }

    public async Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        PendingMoves = validMoves;
        PendingRoll = roll;
        _moveTcs = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (OnMoveRequested is not null)
            await OnMoveRequested.Invoke();

        var move = await _moveTcs.Task;
        _moveTcs = null;
        PendingMoves = [];
        return move;
    }

    public async Task<bool> ShouldSkipAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        PendingMoves = validMoves;
        PendingRoll = roll;
        _skipTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (OnSkipRequested is not null)
            await OnSkipRequested.Invoke();

        var result = await _skipTcs.Task;
        _skipTcs = null;
        PendingMoves = [];
        return result;
    }

    public void SubmitMove(Move move)
    {
        _moveTcs?.TrySetResult(move);
    }

    public void SubmitSkipDecision(bool skip)
    {
        _skipTcs?.TrySetResult(skip);
    }

    public void Cancel()
    {
        _moveTcs?.TrySetCanceled();
        _skipTcs?.TrySetCanceled();
    }
}
