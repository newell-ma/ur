namespace RoyalGameOfUr.Engine;

public sealed class Dice : IDice
{
    private readonly Random _random;

    public Dice() => _random = new Random();

    public Dice(int seed) => _random = new Random(seed);

    public int Roll()
    {
        int sum = 0;
        for (int i = 0; i < 4; i++)
        {
            sum += _random.Next(2); // 0 or 1 per coin
        }
        return sum;
    }
}
