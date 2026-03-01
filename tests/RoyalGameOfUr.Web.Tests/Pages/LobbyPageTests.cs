using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RoyalGameOfUr.Web.Pages;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Tests.Pages;

public class LobbyPageTests : BunitContext
{
    private OnlineGameService _service = null!;

    private IRenderedComponent<LobbyPage> RenderLobby()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            _service = new OnlineGameService(nav, JSInterop.JSRuntime);
            return _service;
        });
        return Render<LobbyPage>();
    }

    private static void SetInRoom(IRenderedComponent<LobbyPage> cut, bool value)
    {
        var field = typeof(LobbyPage).GetField("_inRoom", BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(cut.Instance, value);
    }

    [Test]
    public async Task Dispose_InRoom_GameNotStarted_LeavesRoom()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";
        _service.IsRunning = false;
        SetInRoom(cut, true);

        await cut.Instance.DisposeAsync();

        await Assert.That(_service.RoomCode).IsNull();
    }

    [Test]
    public async Task Dispose_NotInRoom_DoesNothing()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";

        await cut.Instance.DisposeAsync();

        await Assert.That(_service.RoomCode).IsEqualTo("TEST");
    }

    [Test]
    public async Task Dispose_GameStarted_DoesNotLeave()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";
        _service.IsRunning = true;
        SetInRoom(cut, true);

        await cut.Instance.DisposeAsync();

        await Assert.That(_service.RoomCode).IsEqualTo("TEST");
    }

    [Test]
    public async Task Init_ClearsStoredToken()
    {
        var removeInvocation = JSInterop.SetupVoid("sessionStorage.removeItem", "ur_session_token");

        RenderLobby();

        await Assert.That(removeInvocation.Invocations.Count).IsEqualTo(1);
    }
}
