using Bunit;
using RoyalGameOfUr.Web.Components.Board;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Components;

public class CellViewTests : BunitContext
{
    [Test]
    public async Task Renders_BasicCell()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, false));

        var rect = cut.Find("rect");
        await Assert.That(rect).IsNotNull();
        await Assert.That(rect.GetAttribute("class") ?? "").DoesNotContain("rosette");

        await Assert.That(cut.FindAll("polygon")).IsEmpty();
    }

    [Test]
    public async Task Renders_RosetteCell()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, true)
            .Add(p => p.IsValidDestination, false));

        var rect = cut.Find("rect");
        await Assert.That(rect.GetAttribute("class")!).Contains("rosette");

        var polygon = cut.Find("polygon");
        await Assert.That(polygon).IsNotNull();
        await Assert.That(polygon.GetAttribute("class")!).Contains("rosette-star");
    }

    [Test]
    public async Task ValidDestination_HasClass()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, true));

        var rect = cut.Find("rect");
        await Assert.That(rect.GetAttribute("class")!).Contains("valid-destination");
    }

    [Test]
    public async Task Click_FiresCallback_WhenValid()
    {
        bool clicked = false;

        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, true)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("rect").Click();
        await Assert.That(clicked).IsTrue();
    }

    [Test]
    public async Task Click_DoesNotFireCallback_WhenNotValid()
    {
        bool clicked = false;

        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, false)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("rect").Click();
        await Assert.That(clicked).IsFalse();
    }
}
