using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class StartAreaTests : BunitContext
{
    [Test]
    public async Task ShowsCorrectPieceCount()
    {
        var rules = GameRules.Finkel;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, 4)  // one piece on board
            .Build();

        var cut = Render<StartArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, state)
            .Add(p => p.Rules, rules)
            .Add(p => p.ValidMoves, Array.Empty<Move>()));

        var circles = cut.FindAll("circle");
        await Assert.That(circles.Count).IsEqualTo(6);
    }

    [Test]
    public async Task ClickableWhenEnterMoveValid()
    {
        var rules = GameRules.Finkel;
        var state = new GameStateBuilder(rules).Build();

        var validMoves = new List<Move>
        {
            new(Player.One, 0, -1, 3)  // enter from start
        };

        var cut = Render<StartArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, state)
            .Add(p => p.Rules, rules)
            .Add(p => p.ValidMoves, validMoves));

        var circles = cut.FindAll("circle");
        await Assert.That(circles).IsNotEmpty();
        await Assert.That(circles[0].GetAttribute("class")!).Contains("clickable");
    }

    [Test]
    public async Task NotClickableWhenNoEnterMove()
    {
        var rules = GameRules.Finkel;
        var state = new GameStateBuilder(rules).Build();

        var validMoves = new List<Move>
        {
            new(Player.One, 0, 2, 5)  // move on board, not from start
        };

        var cut = Render<StartArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, state)
            .Add(p => p.Rules, rules)
            .Add(p => p.ValidMoves, validMoves));

        var circles = cut.FindAll("circle");
        await Assert.That(circles).IsNotEmpty();
        await Assert.That(circles[0].GetAttribute("class") ?? "").DoesNotContain("clickable");
    }
}
