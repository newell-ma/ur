using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Bunit;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Services;

public class OnlineGameServiceTests : BunitContext
{
    private OnlineGameService CreateService()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        return new OnlineGameService(nav);
    }

    [Fact]
    public async Task LeaveGameAsync_ResetsState()
    {
        var service = CreateService();
        service.RoomCode = "TEST";
        service.IsRunning = true;
        service.OpponentSlow = true;
        service.OpponentDisconnected = true;
        service.IsAwaitingMove = true;
        service.Winner = Player.One;

        await service.LeaveGameAsync();

        Assert.Null(service.RoomCode);
        Assert.False(service.IsRunning);
        Assert.False(service.OpponentSlow);
        Assert.False(service.OpponentDisconnected);
        Assert.False(service.IsAwaitingMove);
        Assert.Null(service.Winner);
    }

    [Fact]
    public async Task LeaveGameAsync_WithoutConnect_DoesNotThrow()
    {
        var service = CreateService();

        // Should not throw even though hub was never connected
        await service.LeaveGameAsync();
    }

    [Fact]
    public void Reset_ClearsDisconnectAndSlowFlags()
    {
        var service = CreateService();
        service.OpponentDisconnected = true;
        service.OpponentSlow = true;

        service.Reset();

        Assert.False(service.OpponentDisconnected);
        Assert.False(service.OpponentSlow);
    }

    [Fact]
    public void Reset_ClearsAllGameState()
    {
        var service = CreateService();
        service.RoomCode = "ABCD";
        service.RulesName = "Finkel";
        service.IsHost = true;
        service.OpponentJoined = true;
        service.OpponentName = "Bob";
        service.IsRunning = true;
        service.IsAwaitingMove = true;
        service.IsAwaitingSkip = true;
        service.Winner = Player.One;
        service.StatusMessage = "test";
        service.ErrorMessage = "err";
        service.DiceRolled = true;
        service.LocalPlayer = Player.One;

        service.Reset();

        Assert.Null(service.RoomCode);
        Assert.Null(service.RulesName);
        Assert.False(service.IsHost);
        Assert.False(service.OpponentJoined);
        Assert.Null(service.OpponentName);
        Assert.False(service.IsRunning);
        Assert.False(service.IsAwaitingMove);
        Assert.False(service.IsAwaitingSkip);
        Assert.Null(service.Winner);
        Assert.Null(service.StatusMessage);
        Assert.Null(service.ErrorMessage);
        Assert.False(service.DiceRolled);
        Assert.Null(service.LocalPlayer);
    }
}
