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

    [Test]
    public async Task LeaveRoomAsync_ClearsStateButKeepsHub()
    {
        var service = CreateService();
        service.RoomCode = "TEST";
        service.IsHost = true;
        service.OpponentJoined = true;
        service.OpponentName = "Bob";

        await service.LeaveRoomAsync();

        await Assert.That(service.RoomCode).IsNull();
        await Assert.That(service.IsHost).IsFalse();
        await Assert.That(service.OpponentJoined).IsFalse();
        await Assert.That(service.OpponentName).IsNull();
    }

    [Test]
    public async Task LeaveRoomAsync_NoRoom_DoesNothing()
    {
        var service = CreateService();

        await service.LeaveRoomAsync();

        await Assert.That(service.RoomCode).IsNull();
    }

    [Test]
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

        await Assert.That(service.RoomCode).IsNull();
        await Assert.That(service.IsRunning).IsFalse();
        await Assert.That(service.OpponentSlow).IsFalse();
        await Assert.That(service.OpponentDisconnected).IsFalse();
        await Assert.That(service.IsAwaitingMove).IsFalse();
        await Assert.That(service.Winner).IsNull();
    }

    [Test]
    public async Task LeaveGameAsync_WithoutConnect_DoesNotThrow()
    {
        var service = CreateService();

        await service.LeaveGameAsync();
    }

    [Test]
    public async Task Reset_ClearsDisconnectAndSlowFlags()
    {
        var service = CreateService();
        service.OpponentDisconnected = true;
        service.OpponentSlow = true;
        service.OpponentReconnecting = true;
        service.SessionToken = "abc123";

        service.Reset();

        await Assert.That(service.OpponentDisconnected).IsFalse();
        await Assert.That(service.OpponentSlow).IsFalse();
        await Assert.That(service.OpponentReconnecting).IsFalse();
        await Assert.That(service.SessionToken).IsNull();
    }

    [Test]
    public async Task Reset_ClearsAllGameState()
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

        await Assert.That(service.RoomCode).IsNull();
        await Assert.That(service.RulesName).IsNull();
        await Assert.That(service.IsHost).IsFalse();
        await Assert.That(service.OpponentJoined).IsFalse();
        await Assert.That(service.OpponentName).IsNull();
        await Assert.That(service.IsRunning).IsFalse();
        await Assert.That(service.IsAwaitingMove).IsFalse();
        await Assert.That(service.IsAwaitingSkip).IsFalse();
        await Assert.That(service.Winner).IsNull();
        await Assert.That(service.StatusMessage).IsNull();
        await Assert.That(service.ErrorMessage).IsNull();
        await Assert.That(service.DiceRolled).IsFalse();
        await Assert.That(service.LocalPlayer).IsNull();
    }

    [Test]
    public async Task TryRejoinAsync_NoStoredToken_ReturnsFalse()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        JSInterop.Setup<string?>("sessionStorage.getItem", "ur_session_token").SetResult(null);
        var service = new OnlineGameService(nav, JSInterop.JSRuntime);

        var result = await service.TryRejoinAsync();

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ClearStoredTokenAsync_RemovesFromSessionStorage()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var removeInvocation = JSInterop.SetupVoid("sessionStorage.removeItem", "ur_session_token");
        removeInvocation.SetVoidResult();
        var service = new OnlineGameService(nav, JSInterop.JSRuntime);

        await service.ClearStoredTokenAsync();

        await Assert.That(removeInvocation.Invocations.Count).IsEqualTo(1);
    }
}
