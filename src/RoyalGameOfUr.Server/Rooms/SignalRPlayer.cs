using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Server.Rooms;

public sealed class SignalRPlayer : ISkipCapablePlayer
{
    private TaskCompletionSource<Move>? _moveTcs;
    private TaskCompletionSource<bool>? _skipTcs;

    public string Name { get; }
    public string ConnectionId { get; set; }
    public IReadOnlyList<Move> PendingMoves { get; private set; } = [];

    /// <summary>
    /// Called when the player needs to choose a move. GameRoom wires this to send SignalR message.
    /// </summary>
    public Func<IReadOnlyList<Move>, int, Task>? OnMoveRequired { get; set; }

    /// <summary>
    /// Called when the player needs to decide whether to skip. GameRoom wires this to send SignalR message.
    /// </summary>
    public Func<IReadOnlyList<Move>, int, Task>? OnSkipRequired { get; set; }

    public SignalRPlayer(string name, string connectionId)
    {
        Name = name;
        ConnectionId = connectionId;
    }

    public async Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        PendingMoves = validMoves;
        _moveTcs = new TaskCompletionSource<Move>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (OnMoveRequired is not null)
            await OnMoveRequired(validMoves, roll);

        var move = await _moveTcs.Task;
        _moveTcs = null;
        PendingMoves = [];
        return move;
    }

    public async Task<bool> ShouldSkipAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        PendingMoves = validMoves;
        _skipTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (OnSkipRequired is not null)
            await OnSkipRequired(validMoves, roll);

        var result = await _skipTcs.Task;
        _skipTcs = null;
        PendingMoves = [];
        return result;
    }

    public bool TrySubmitMove(Move move)
    {
        if (_moveTcs is null) return false;
        if (!PendingMoves.Contains(move)) return false;
        _moveTcs.TrySetResult(move);
        return true;
    }

    public bool TrySubmitSkipDecision(bool skip)
    {
        if (_skipTcs is null) return false;
        _skipTcs.TrySetResult(skip);
        return true;
    }

    public void Cancel()
    {
        _moveTcs?.TrySetCanceled();
        _skipTcs?.TrySetCanceled();
    }

    public bool IsAwaitingMove => _moveTcs is not null;
    public bool IsAwaitingSkip => _skipTcs is not null;
}
