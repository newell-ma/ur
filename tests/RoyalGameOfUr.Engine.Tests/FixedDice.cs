using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Engine.Tests;

public sealed class FixedDice : IDice
{
    private readonly Queue<int> _rolls;

    public FixedDice(params int[] rolls)
    {
        _rolls = new Queue<int>(rolls);
    }

    public int Roll()
    {
        if (_rolls.Count == 0)
            throw new InvalidOperationException("No more predetermined rolls.");
        return _rolls.Dequeue();
    }

    public int Remaining => _rolls.Count;
}
