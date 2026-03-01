using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class PieceViewTests : BunitContext
{
    [Test]
    public async Task Renders_Player1Piece()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One));

        var circle = cut.Find("circle");
        await Assert.That(circle.GetAttribute("class")!).Contains("piece-circle-p1");
    }

    [Test]
    public async Task Renders_Player2Piece()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.Two));

        var circle = cut.Find("circle");
        await Assert.That(circle.GetAttribute("class")!).Contains("piece-circle-p2");
    }

    [Test]
    public async Task StackCount_Shown()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.ShowStackCount, true)
            .Add(p => p.StackCount, 3));

        var text = cut.Find("text");
        await Assert.That(text.TextContent).IsEqualTo("3");
    }

    [Test]
    public async Task StackCount_HiddenWhenOne()
    {
        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.ShowStackCount, true)
            .Add(p => p.StackCount, 1));

        await Assert.That(cut.FindAll("text")).IsEmpty();
    }

    [Test]
    public async Task Click_DoesNotFire_WhenNotClickable()
    {
        bool clicked = false;

        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.IsClickable, false)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("g").Click();
        await Assert.That(clicked).IsFalse();
    }

    [Test]
    public async Task Click_Fires_WhenClickable()
    {
        bool clicked = false;

        var cut = Render<PieceView>(parameters => parameters
            .Add(p => p.CenterX, 50)
            .Add(p => p.CenterY, 50)
            .Add(p => p.Player, Player.One)
            .Add(p => p.IsClickable, true)
            .Add(p => p.OnClick, () => { clicked = true; }));

        cut.Find("g").Click();
        await Assert.That(clicked).IsTrue();
    }
}
