namespace RoyalGameOfUr.Engine;

public sealed class Dice : IDice
{
    private readonly Random _random;
    private readonly int _count;

    public Dice() : this(null, 4) { }

    public Dice(int seed) : this(seed, 4) { }

    public Dice(int? seed, int count)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _count = count;
    }

    public int Roll()
    {
        int sum = 0;
        for (int i = 0; i < _count; i++)
        {
            sum += _random.Next(2); // 0 or 1 per coin
        }
        return sum;
    }
}
