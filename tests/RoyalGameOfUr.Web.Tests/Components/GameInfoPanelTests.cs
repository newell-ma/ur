using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Info;

namespace RoyalGameOfUr.Web.Tests.Components;

public class GameInfoPanelTests : BunitContext
{
    [Test]
    public async Task ShowsPlayerNames()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Alice")
            .Add(p => p.Player2Name, "Bob"));

        await Assert.That(cut.Markup).Contains("Alice");
        await Assert.That(cut.Markup).Contains("Bob");
    }

    [Test]
    public async Task ShowsAiLabel()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Human")
            .Add(p => p.Player2Name, "Bot")
            .Add(p => p.Player1Type, PlayerType.Human)
            .Add(p => p.Player2Type, PlayerType.Computer));

        await Assert.That(cut.Markup).Contains("(AI)");

        var aiLabels = cut.FindAll(".player-type");
        await Assert.That(aiLabels.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ShowsTurnIndicator()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithCurrentPlayer(Player.One)
            .Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Alice")
            .Add(p => p.Player2Name, "Bob"));

        var turnIndicator = cut.Find(".turn-indicator");
        await Assert.That(turnIndicator.TextContent).Contains("Alice");
        await Assert.That(turnIndicator.TextContent).Contains("turn");
    }

    [Test]
    public async Task ShowsStatusMessage()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "P1")
            .Add(p => p.Player2Name, "P2")
            .Add(p => p.StatusMessage, "P1 rolled 3"));

        var statusDiv = cut.Find(".status-message");
        await Assert.That(statusDiv.TextContent).IsEqualTo("P1 rolled 3");
    }
}
