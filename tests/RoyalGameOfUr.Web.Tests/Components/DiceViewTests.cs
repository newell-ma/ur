using Bunit;
using RoyalGameOfUr.Web.Components.Dice;

namespace RoyalGameOfUr.Web.Tests.Components;

public class DiceViewTests : BunitContext
{
    [Test]
    public async Task RendersDice()
    {
        var dice = new[] { 1, 0, 1, 0 };

        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, dice)
            .Add(p => p.Total, 2)
            .Add(p => p.EffectiveTotal, 2));

        var svgs = cut.FindAll("svg.die");
        await Assert.That(svgs.Count).IsEqualTo(4);
    }

    [Test]
    public async Task MarkedDice_HasClass()
    {
        var dice = new[] { 1, 0, 1, 0 };

        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, dice)
            .Add(p => p.Total, 2)
            .Add(p => p.EffectiveTotal, 2));

        var tips = cut.FindAll(".die-tip");
        await Assert.That(tips.Count).IsEqualTo(4);

        await Assert.That(tips[0].GetAttribute("class")!).Contains("marked");
        await Assert.That(tips[1].GetAttribute("class")!).DoesNotContain("marked");
    }

    [Test]
    public async Task RollingAnimation_HasClass()
    {
        var dice = new[] { 1, 0, 1, 0 };

        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, dice)
            .Add(p => p.Total, 2)
            .Add(p => p.EffectiveTotal, 2)
            .Add(p => p.IsRolling, true));

        var svgs = cut.FindAll("svg.die");
        foreach (var svg in svgs)
        {
            await Assert.That(svg.GetAttribute("class")!).Contains("rolling");
        }
    }

    [Test]
    public async Task NullDice_RendersNothing()
    {
        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, (int[]?)null)
            .Add(p => p.Total, 0)
            .Add(p => p.EffectiveTotal, 0));

        await Assert.That(cut.FindAll("svg.die")).IsEmpty();
    }
}
