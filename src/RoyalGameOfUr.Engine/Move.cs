namespace RoyalGameOfUr.Engine;

public readonly record struct Move(Player Player, int PieceIndex, int From, int To);
