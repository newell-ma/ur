using RoyalGameOfUr.Engine;

namespace RoyalGameOfUr.Engine.Tests;

public class DiceTests
{
    [Test]
    public async Task Roll_ReturnsValueInRange0To4()
    {
        var dice = new Dice();
        var results = new HashSet<int>();

        for (int i = 0; i < 1000; i++)
        {
            int roll = dice.Roll();
            await Assert.That(roll).IsGreaterThanOrEqualTo(0);
            await Assert.That(roll).IsLessThanOrEqualTo(4);
            results.Add(roll);
        }

        // With 1000 rolls we should see all possible values
        await Assert.That(results).Contains(0);
        await Assert.That(results).Contains(4);
    }

    [Test]
    public async Task Roll_WithSameSeed_ProducesDeterministicResults()
    {
        var dice1 = new Dice(42);
        var dice2 = new Dice(42);

        for (int i = 0; i < 100; i++)
        {
            await Assert.That(dice1.Roll()).IsEqualTo(dice2.Roll());
        }
    }

    [Test]
    public async Task FixedDice_ReturnsQueuedValues()
    {
        var dice = new FixedDice(1, 3, 0, 4);

        await Assert.That(dice.Roll()).IsEqualTo(1);
        await Assert.That(dice.Roll()).IsEqualTo(3);
        await Assert.That(dice.Roll()).IsEqualTo(0);
        await Assert.That(dice.Roll()).IsEqualTo(4);
    }

    [Test]
    public void FixedDice_ThrowsWhenExhausted()
    {
        var dice = new FixedDice(1);
        dice.Roll();

        Assert.Throws<InvalidOperationException>(() => dice.Roll());
    }
}
