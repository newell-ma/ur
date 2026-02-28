using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class BorneOffAreaTests : BunitContext
{
    [Fact]
    public void ShowsCorrectCount()
    {
        var rules = GameRules.Finkel;
        var state = new GameStateBuilder(rules)
            .WithPiece(Player.One, rules.PathLength) // borne off
            .WithPiece(Player.One, rules.PathLength) // borne off
            .Build();

        var cut = Render<BorneOffArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, state)
            .Add(p => p.Rules, rules));

        // Should show "P1 Home: 2 / 7"
        cut.MarkupMatches(cut.Markup); // sanity
        Assert.Contains("2 / 7", cut.Markup);
    }

    [Fact]
    public void NullState_RendersNothing()
    {
        var cut = Render<BorneOffArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, (GameState?)null)
            .Add(p => p.Rules, GameRules.Finkel));

        Assert.Empty(cut.Markup.Trim());
    }
}
