using Bunit;
using RoyalGameOfUr.Web.Components.Board;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Components;

public class CellViewTests : BunitContext
{
    [Fact]
    public void Renders_BasicCell()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, false));

        var rect = cut.Find("rect");
        Assert.NotNull(rect);
        Assert.DoesNotContain("rosette", rect.GetAttribute("class") ?? "");

        // No star polygon for non-rosette
        Assert.Empty(cut.FindAll("polygon"));
    }

    [Fact]
    public void Renders_RosetteCell()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, true)
            .Add(p => p.IsValidDestination, false));

        var rect = cut.Find("rect");
        Assert.Contains("rosette", rect.GetAttribute("class")!);

        // Should have star polygon
        var polygon = cut.Find("polygon");
        Assert.NotNull(polygon);
        Assert.Contains("rosette-star", polygon.GetAttribute("class")!);
    }

    [Fact]
    public void ValidDestination_HasClass()
    {
        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, true));

        var rect = cut.Find("rect");
        Assert.Contains("valid-destination", rect.GetAttribute("class")!);
    }

    [Fact]
    public void Click_FiresCallback_WhenValid()
    {
        bool clicked = false;

        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, true)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("rect").Click();
        Assert.True(clicked);
    }

    [Fact]
    public void Click_DoesNotFireCallback_WhenNotValid()
    {
        bool clicked = false;

        var cut = Render<CellView>(parameters => parameters
            .Add(p => p.X, 20)
            .Add(p => p.Y, 20)
            .Add(p => p.IsRosette, false)
            .Add(p => p.IsValidDestination, false)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("rect").Click();
        Assert.False(clicked);
    }
}
