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
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            var service = new OnlineGameService(nav, JSInterop.JSRuntime);
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

    [Test]
    public async Task OpponentDisconnected_ShowsDisconnectBanner()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        var banner = cut.Find(".disconnect-banner");
        await Assert.That(banner.TextContent.ToLowerInvariant()).Contains("disconnected");
    }

    [Test]
    public async Task OpponentDisconnected_ShowsBackToLobbyButton()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        var button = cut.Find(".disconnect-banner button");
        await Assert.That(button.TextContent).IsEqualTo("Back to Lobby");
    }

    [Test]
    public async Task OpponentDisconnected_BackToLobby_Navigates()
    {
        var cut = RenderWithService(s => s.OpponentDisconnected = true);

        cut.Find(".disconnect-banner button").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        await Assert.That(nav.Uri).EndsWith("/online");
    }

    [Test]
    public async Task OpponentSlow_ShowsSlowBanner()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        var banner = cut.Find(".slow-banner");
        await Assert.That(banner.TextContent).Contains("taking a while");
    }

    [Test]
    public async Task OpponentSlow_ShowsLeaveGameButton()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        var button = cut.Find(".slow-banner .btn-leave");
        await Assert.That(button.TextContent).IsEqualTo("Leave Game");
    }

    [Test]
    public async Task OpponentSlow_LeaveGame_Navigates()
    {
        var cut = RenderWithService(s => s.OpponentSlow = true);

        cut.Find(".slow-banner .btn-leave").Click();

        var nav = Services.GetRequiredService<NavigationManager>();
        await Assert.That(nav.Uri).EndsWith("/online");
    }

    [Test]
    public async Task OpponentDisconnected_HidesSlowBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentSlow = true;
            s.OpponentDisconnected = true;
        });

        await Assert.That(cut.Find(".disconnect-banner")).IsNotNull();
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
    }

    [Test]
    public async Task NoTimeout_NoDisconnect_ShowsWaitingMessage()
    {
        var cut = RenderWithService(s =>
        {
            s.State = new GameStateBuilder(GameRules.Finkel)
                .WithCurrentPlayer(Player.Two)
                .Build();
            s.LocalPlayer = Player.One;
        });

        var waiting = cut.Find(".waiting-turn");
        await Assert.That(waiting.TextContent).Contains("Waiting for opponent");
    }

    [Test]
    public async Task OpponentSlow_HidesWaitingMessage()
    {
        var cut = RenderWithService(s =>
        {
            s.State = new GameStateBuilder(GameRules.Finkel)
                .WithCurrentPlayer(Player.Two)
                .Build();
            s.LocalPlayer = Player.One;
            s.OpponentSlow = true;
        });

        await Assert.That(cut.Find(".slow-banner")).IsNotNull();
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".waiting-turn"));
    }

    [Test]
    public void NormalState_NoBanners()
    {
        var cut = RenderWithService(_ => { });

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".disconnect-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }

    [Test]
    public async Task OpponentReconnecting_ShowsReconnectingBanner()
    {
        var cut = RenderWithService(s => s.OpponentReconnecting = true);

        var banner = cut.Find(".reconnecting-banner");
        await Assert.That(banner.TextContent.ToLowerInvariant()).Contains("reconnecting");
    }

    [Test]
    public async Task OpponentReconnecting_HidesOnReconnect()
    {
        OnlineGameService? service = null;
        var cut = RenderWithService(s =>
        {
            s.OpponentReconnecting = true;
            service = s;
        });

        await Assert.That(cut.Find(".reconnecting-banner")).IsNotNull();

        service!.OpponentReconnecting = false;
        cut.Render();

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }

    [Test]
    public async Task OpponentReconnecting_HidesSlowBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentSlow = true;
            s.OpponentReconnecting = true;
        });

        await Assert.That(cut.Find(".reconnecting-banner")).IsNotNull();
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".slow-banner"));
    }

    [Test]
    public async Task OpponentDisconnected_HidesReconnectingBanner()
    {
        var cut = RenderWithService(s =>
        {
            s.OpponentReconnecting = true;
            s.OpponentDisconnected = true;
        });

        await Assert.That(cut.Find(".disconnect-banner")).IsNotNull();
        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-banner"));
    }

    [Test]
    public async Task Refresh_NoStoredToken_RedirectsToLobby()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("sessionStorage.getItem", "ur_session_token").SetResult(null);
        Services.AddSingleton(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new OnlineGameService(nav, JSInterop.JSRuntime);
        });

        Render<OnlineGamePage>();

        var nav = Services.GetRequiredService<NavigationManager>();
        await Assert.That(nav.Uri).EndsWith("/online");
    }

    [Test]
    public void NormalState_NoReconnectingOverlay()
    {
        var cut = RenderWithService(_ => { });

        Assert.Throws<Bunit.ElementNotFoundException>(() => cut.Find(".reconnecting-overlay"));
    }
}
