using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Info;

namespace RoyalGameOfUr.Web.Tests.Components;

public class GameInfoPanelTests : BunitContext
{
    [Fact]
    public void ShowsPlayerNames()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Alice")
            .Add(p => p.Player2Name, "Bob"));

        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("Bob", cut.Markup);
    }

    [Fact]
    public void ShowsAiLabel()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Human")
            .Add(p => p.Player2Name, "Bot")
            .Add(p => p.Player1Type, PlayerType.Human)
            .Add(p => p.Player2Type, PlayerType.Computer));

        Assert.Contains("(AI)", cut.Markup);

        // Only P2 should have AI label
        var aiLabels = cut.FindAll(".player-type");
        Assert.Single(aiLabels);
    }

    [Fact]
    public void ShowsTurnIndicator()
    {
        var state = new GameStateBuilder(GameRules.Finkel)
            .WithCurrentPlayer(Player.One)
            .Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "Alice")
            .Add(p => p.Player2Name, "Bob"));

        var turnIndicator = cut.Find(".turn-indicator");
        Assert.Contains("Alice", turnIndicator.TextContent);
        Assert.Contains("turn", turnIndicator.TextContent);
    }

    [Fact]
    public void ShowsStatusMessage()
    {
        var state = new GameStateBuilder(GameRules.Finkel).Build();

        var cut = Render<GameInfoPanel>(parameters => parameters
            .Add(p => p.State, state)
            .Add(p => p.Player1Name, "P1")
            .Add(p => p.Player2Name, "P2")
            .Add(p => p.StatusMessage, "P1 rolled 3"));

        var statusDiv = cut.Find(".status-message");
        Assert.Equal("P1 rolled 3", statusDiv.TextContent);
    }
}
