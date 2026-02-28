using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class StartAreaTests : BunitContext
{
    [Fact]
    public void ShowsCorrectPieceCount()
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

        // 7 pieces total, 1 on board = 6 at start
        var circles = cut.FindAll("circle");
        Assert.Equal(6, circles.Count);
    }

    [Fact]
    public void ClickableWhenEnterMoveValid()
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
        Assert.NotEmpty(circles);
        Assert.Contains("clickable", circles[0].GetAttribute("class")!);
    }

    [Fact]
    public void NotClickableWhenNoEnterMove()
    {
        var rules = GameRules.Finkel;
        var state = new GameStateBuilder(rules).Build();

        // No valid moves from start
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
        Assert.NotEmpty(circles);
        Assert.DoesNotContain("clickable", circles[0].GetAttribute("class") ?? "");
    }
}
