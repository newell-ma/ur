using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Info;

namespace RoyalGameOfUr.Web.Tests.Components;

public class GameOverOverlayTests : BunitContext
{
    [Fact]
    public void ShowsWinnerName()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice"));

        Assert.Contains("Alice wins!", cut.Markup);
        Assert.Contains("Game Over", cut.Markup);
    }

    [Fact]
    public void PlayAgainButton_FiresCallback()
    {
        bool playAgainClicked = false;

        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice")
            .Add(p => p.OnPlayAgain, () => { playAgainClicked = true; }));

        cut.Find("button").Click();
        Assert.True(playAgainClicked);
    }

    [Fact]
    public void CorrectPlayerClass()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice"));

        var winnerElement = cut.Find(".game-over-winner");
        Assert.Contains("text-player1", winnerElement.GetAttribute("class")!);
    }

    [Fact]
    public void Player2_HasCorrectClass()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.Two)
            .Add(p => p.WinnerName, "Bob"));

        var winnerElement = cut.Find(".game-over-winner");
        Assert.Contains("text-player2", winnerElement.GetAttribute("class")!);
    }
}
