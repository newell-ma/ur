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
        JSInterop.Mode = JSRuntimeMode.Loose;
        return new OnlineGameService(nav, JSInterop.JSRuntime);
    }

    [Fact]
    public async Task LeaveRoomAsync_ClearsStateButKeepsHub()
    {
        var service = CreateService();
        service.RoomCode = "TEST";
        service.IsHost = true;
        service.OpponentJoined = true;
        service.OpponentName = "Bob";

        // Hub is null (not connected), so SendAsync is skipped but Reset still runs
        await service.LeaveRoomAsync();

        Assert.Null(service.RoomCode);
        Assert.False(service.IsHost);
        Assert.False(service.OpponentJoined);
        Assert.Null(service.OpponentName);
    }

    [Fact]
    public async Task LeaveRoomAsync_NoRoom_DoesNothing()
    {
        var service = CreateService();

        // Should not throw when RoomCode is null
        await service.LeaveRoomAsync();

        Assert.Null(service.RoomCode);
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
        service.OpponentReconnecting = true;
        service.SessionToken = "abc123";

        service.Reset();

        Assert.False(service.OpponentDisconnected);
        Assert.False(service.OpponentSlow);
        Assert.False(service.OpponentReconnecting);
        Assert.Null(service.SessionToken);
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

    [Fact]
    public async Task TryRejoinAsync_NoStoredToken_ReturnsFalse()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        JSInterop.Setup<string?>("sessionStorage.getItem", "ur_session_token").SetResult(null);
        var service = new OnlineGameService(nav, JSInterop.JSRuntime);

        var result = await service.TryRejoinAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task ClearStoredTokenAsync_RemovesFromSessionStorage()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var removeInvocation = JSInterop.SetupVoid("sessionStorage.removeItem", "ur_session_token");
        removeInvocation.SetVoidResult();
        var service = new OnlineGameService(nav, JSInterop.JSRuntime);

        await service.ClearStoredTokenAsync();

        Assert.Single(removeInvocation.Invocations);
    }
}
