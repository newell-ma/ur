using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Components.Info;

namespace RoyalGameOfUr.Web.Tests.Components;

public class GameOverOverlayTests : BunitContext
{
    [Test]
    public async Task ShowsWinnerName()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice"));

        await Assert.That(cut.Markup).Contains("Alice wins!");
        await Assert.That(cut.Markup).Contains("Game Over");
    }

    [Test]
    public async Task PlayAgainButton_FiresCallback()
    {
        bool playAgainClicked = false;

        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice")
            .Add(p => p.OnPlayAgain, () => { playAgainClicked = true; }));

        cut.Find("button").Click();
        await Assert.That(playAgainClicked).IsTrue();
    }

    [Test]
    public async Task CorrectPlayerClass()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.One)
            .Add(p => p.WinnerName, "Alice"));

        var winnerElement = cut.Find(".game-over-winner");
        await Assert.That(winnerElement.GetAttribute("class")!).Contains("text-player1");
    }

    [Test]
    public async Task Player2_HasCorrectClass()
    {
        var cut = Render<GameOverOverlay>(parameters => parameters
            .Add(p => p.Winner, Player.Two)
            .Add(p => p.WinnerName, "Bob"));

        var winnerElement = cut.Find(".game-over-winner");
        await Assert.That(winnerElement.GetAttribute("class")!).Contains("text-player2");
    }
}
