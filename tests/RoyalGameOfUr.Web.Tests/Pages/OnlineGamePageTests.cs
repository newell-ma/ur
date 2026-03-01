using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Pages;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Pages;

public class OnlineGamePageTests : BunitContext
{
    private IRenderedComponent<OnlineGamePage> RenderWithService(Action<OnlineGameService> configure)
    {
        Services.AddSingleton(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            var service = new OnlineGameService(nav);
            // Minimum state so OnInitialized doesn't redirect away
            service.Rules = GameRules.Finkel;
            service.IsRunning = true;
            service.RoomCode = "TEST";
            service.LocalPlayer = Player.One;
            service.Player1Name = "Alice";
            service.Player2Name = "Bob";
            service.State = new GameStateBuilder(GameRules.Finkel).Build();
            configure(service);
            return service;
        });
        return Render<OnlineGamePage>();
    }

    [Fact]
    public void OpponentDisconnected_ShowsDisconnectBanner()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        var banner = cut.Find(".disconnect-banner");
        Assert.Contains("disconnected", banner.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpponentDisconnected_ShowsBackToLobbyButton()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        var button = cut.Find(".disconnect-banner button");
        Assert.Equal("Back to Lobby", button.TextContent);
    }

    [Fact]
    public void OpponentDisconnected_BackToLobby_Navigates()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        cut.Find(".disconnect-banner button").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/online", nav.Uri);
    }

    [Fact]
    public void OpponentSlow_ShowsSlowBanner()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        var banner = cut.Find(".slow-banner");
        Assert.Contains("taking a while", banner.TextContent);
    }

    [Fact]
    public void OpponentSlow_ShowsLeaveGameButton()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        var button = cut.Find(".slow-banner .btn-leave");
        Assert.Equal("Leave Game", button.TextContent);
    }

    [Fact]
    public void OpponentSlow_LeaveGame_Navigates()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        cut.Find(".slow-banner .btn-leave").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/online", nav.Uri);
    }

    [Fact]
    public void OpponentDisconnected_HidesSlowBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentSlow = true;
            s.OpponentDisconnected = true;
        });

        Assert.NotNull(cut.Find(".disconnect-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
    }

    [Fact]
    public void NoTimeout_NoDisconnect_ShowsWaitingMessage()
    {
        var cut = RenderWithService(s =>
        {
            s.State = new GameStateBuilder(GameRules.Finkel)
                .WithCurrentPlayer(Player.Two)
                .Build();
            s.LocalPlayer = Player.One;
        });

        var waiting = cut.Find(".waiting-turn");
        Assert.Contains("Waiting for opponent", waiting.TextContent);
    }

    [Fact]
    public void OpponentSlow_HidesWaitingMessage()
    {
        var cut = RenderWithService(s =>
        {
            s.State = new GameStateBuilder(GameRules.Finkel)
                .WithCurrentPlayer(Player.Two)
                .Build();
            s.LocalPlayer = Player.One;
            s.OpponentSlow = true;
        });

        Assert.NotNull(cut.Find(".slow-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".waiting-turn"));
    }

    [Fact]
    public void NormalState_NoBanners()
    {
        var cut = RenderWithService(_ => { });

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".disconnect-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }

    [Fact]
    public void OpponentReconnecting_ShowsReconnectingBanner()
    {
        var cut = RenderWithService(s => s.OpponentReconnecting = true);

        var banner = cut.Find(".reconnecting-banner");
        Assert.Contains("reconnecting", banner.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpponentReconnecting_HidesOnReconnect()
    {
        OnlineGameService? service = null;
        var cut = RenderWithService(s =>
        {
            s.OpponentReconnecting = true;
            service = s;
        });

        // Banner visible
        Assert.NotNull(cut.Find(".reconnecting-banner"));

        // Simulate reconnection
        service!.OpponentReconnecting = false;
        cut.Render();

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }

    [Fact]
    public void OpponentReconnecting_HidesSlowBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentSlow = true;
            s.OpponentReconnecting = true;
        });

        Assert.NotNull(cut.Find(".reconnecting-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
    }

    [Fact]
    public void OpponentDisconnected_HidesReconnectingBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentReconnecting = true;
            s.OpponentDisconnected = true;
        });

        Assert.NotNull(cut.Find(".disconnect-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }
}
