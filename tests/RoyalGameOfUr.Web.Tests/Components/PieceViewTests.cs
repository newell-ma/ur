using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class PieceViewTests : BunitContext
{
    [Fact]
    public void Renders_Player1Piece()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One));

        var circle = cut.Find("circle");
        Assert.Contains("piece-circle-p1", circle.GetAttribute("class")!);
    }

    [Fact]
    public void Renders_Player2Piece()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.Two));

        var circle = cut.Find("circle");
        Assert.Contains("piece-circle-p2", circle.GetAttribute("class")!);
    }

    [Fact]
    public void StackCount_Shown()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.ShowStackCount, true)
            .Add(p => p.StackCount, 3));

        var text = cut.Find("text");
        Assert.Equal("3", text.TextContent);
    }

    [Fact]
    public void StackCount_HiddenWhenOne()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.ShowStackCount, true)
            .Add(p => p.StackCount, 1));

        Assert.Empty(cut.FindAll("text"));
    }

    [Fact]
    public void Click_DoesNotFire_WhenNotClickable()
    {
        bool clicked = false;

        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.IsClickable, false)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("g").Click();
        Assert.False(clicked);
    }

    [Fact]
    public void Click_Fires_WhenClickable()
    {
        bool clicked = false;

        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.IsClickable, true)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("g").Click();
        Assert.True(clicked);
    }
}
