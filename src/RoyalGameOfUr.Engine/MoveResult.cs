namespace RoyalGameOfUr.Engine;

public enum MoveResult
{
    Moved,
    ExtraTurn,
    Captured,
    CapturedAndExtraTurn,
    BorneOff,
    BorneOffAndExtraTurn,
    Win
}

public readonly record struct MoveOutcome(MoveResult Result, int CapturedPieceIndex = -1)
{
    public bool HasCapture => CapturedPieceIndex >= 0;
}
