using Bunit;
using RoyalGameOfUr.Web.Components.Dice;

namespace RoyalGameOfUr.Web.Tests.Components;

public class DiceViewTests : BunitContext
{
    [Fact]
    public void RendersDice()
    {
        var dice = new[] { 1, 0, 1, 0 };

        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, dice)
            .Add(p => p.Total, 2)
            .Add(p => p.EffectiveTotal, 2));

        var svgs = cut.FindAll("svg.die");
        Assert.Equal(4, svgs.Count);
    }

    [Fact]
    public void MarkedDice_HasClass()
    {
        var dice = new[] { 1, 0, 1, 0 };

        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, dice)
            .Add(p => p.Total, 2)
            .Add(p => p.EffectiveTotal, 2));

        var tips = cut.FindAll(".die-tip");
        Assert.Equal(4, tips.Count);

        // First die (value=1) should be marked
        Assert.Contains("marked", tips[0].GetAttribute("class")!);
        // Second die (value=0) should not be marked
        Assert.DoesNotContain("marked", tips[1].GetAttribute("class")!);
    }

    [Fact]
    public void RollingAnimation_HasClass()
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
            Assert.Contains("rolling", svg.GetAttribute("class")!);
        }
    }

    [Fact]
    public void NullDice_RendersNothing()
    {
        var cut = Render<DiceView>(parameters => parameters
            .Add(p => p.IndividualDice, (int[]?)null)
            .Add(p => p.Total, 0)
            .Add(p => p.EffectiveTotal, 0));

        Assert.Empty(cut.FindAll("svg.die"));
    }
}
