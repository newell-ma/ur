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

    [Fact]
    public async Task Dispose_InRoom_GameNotStarted_LeavesRoom()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";
        _service.IsRunning = false;
        SetInRoom(cut, true);

        await cut.Instance.DisposeAsync();

        // LeaveRoomAsync should have been called, which resets RoomCode
        Assert.Null(_service.RoomCode);
    }

    [Fact]
    public async Task Dispose_NotInRoom_DoesNothing()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";
        // _inRoom is false (default)

        await cut.Instance.DisposeAsync();

        // Should not call LeaveRoomAsync, so RoomCode stays
        Assert.Equal("TEST", _service.RoomCode);
    }

    [Fact]
    public async Task Dispose_GameStarted_DoesNotLeave()
    {
        var cut = RenderLobby();

        _service.RoomCode = "TEST";
        _service.IsRunning = true;
        SetInRoom(cut, true);

        await cut.Instance.DisposeAsync();

        // Should NOT call LeaveRoomAsync because game is running
        Assert.Equal("TEST", _service.RoomCode);
    }

    [Fact]
    public void Init_ClearsStoredToken()
    {
        var removeInvocation = JSInterop.SetupVoid("sessionStorage.removeItem", "ur_session_token");

        RenderLobby();

        Assert.Single(removeInvocation.Invocations);
    }
}
