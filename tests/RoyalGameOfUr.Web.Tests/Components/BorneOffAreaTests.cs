using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Pieces;

namespace RoyalGameOfUr.Web.Tests.Components;

public class BorneOffAreaTests : BunitContext
{
    [Test]
    public async Task ShowsCorrectCount()
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

        await Assert.That(cut.Markup).Contains("2 / 7");
    }

    [Test]
    public async Task NullState_RendersNothing()
    {
        var cut = Render<BorneOffArea>(parameters => parameters
            .Add(p => p.Player, Player.One)
            .Add(p => p.State, (GameState?)null)
            .Add(p => p.Rules, GameRules.Finkel));

        await Assert.That(cut.Markup.Trim()).IsEmpty();
    }
}
