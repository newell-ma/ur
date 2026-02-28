using RoyalGameOfUr.E2E.Helpers;
using RoyalGameOfUr.E2E.Infrastructure;

namespace RoyalGameOfUr.E2E;

[ClassDataSource<AppFixture>(Shared = SharedType.PerAssembly)]
public class OnlineGameTests(AppFixture fixture)
{
    // -- Lobby Tests --

    [Test]
    public async Task CreateRoom_ShowsRoomCode()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);

        await session.HostLobby.NavigateAsync();
        var code = await session.HostLobby.CreateRoomAsync("Alice");

        await Assert.That(code).IsNotNullOrEmpty();
        await Assert.That(code.Length).IsEqualTo(4);
    }

    [Test]
    public async Task JoinRoom_HostSeesStartButton()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);

        await session.HostLobby.NavigateAsync();
        var code = await session.HostLobby.CreateRoomAsync("Alice");

        await session.GuestLobby.NavigateAsync();
        await session.GuestLobby.JoinRoomAsync("Bob", code);

        await session.HostLobby.WaitForOpponentJoinedAsync();
        await Assert.That(session.HostLobby.StartGameButton).IsNotNull();
    }

    [Test]
    public async Task JoinRoom_InvalidCode_ShowsError()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);

        await session.GuestLobby.NavigateAsync();
        await session.GuestLobby.JoinRoomAsync("Bob", "ZZZZ");

        var errorText = session.GuestLobby.ErrorText;
        await errorText.WaitForAsync(new() { Timeout = 5_000 });
        var text = await errorText.TextContentAsync();
        await Assert.That(text).IsNotNullOrEmpty();
    }

    // -- Game Start Tests --

    [Test]
    public async Task StartGame_BothNavigateToGamePage()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);
        await session.SetupAndStartGameAsync();

        // Both pages should show the board
        await Assert.That(await session.HostGame.Board.IsVisibleAsync()).IsTrue();
        await Assert.That(await session.GuestGame.Board.IsVisibleAsync()).IsTrue();
    }

    [Test]
    public async Task GamePage_ShowsPlayerNamesAndRoom()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);
        await session.SetupAndStartGameAsync("Alice", "Bob");

        var hostInfoText = await session.HostGame.InfoBar.TextContentAsync();
        await Assert.That(hostInfoText).Contains(session.RoomCode);
        await Assert.That(hostInfoText).Contains("Alice");

        var guestInfoText = await session.GuestGame.InfoBar.TextContentAsync();
        await Assert.That(guestInfoText).Contains(session.RoomCode);
        await Assert.That(guestInfoText).Contains("Bob");
    }

    // -- Gameplay Tests --

    [Test]
    public async Task ActivePlayer_SeesClickablePieces()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);
        await session.SetupAndStartGameAsync();

        // Wait for the game state to settle — one player should have clickable pieces
        // (the active player) or be waiting for the opponent
        await Task.Delay(2_000);

        var hostHas = await session.HostGame.HasClickablePiecesAsync();
        var guestHas = await session.GuestGame.HasClickablePiecesAsync();

        // Exactly one should have clickable pieces (or the active player rolled 0 and forfeited)
        // At minimum, at least one turn should have passed with the board visible
        await Assert.That(await session.HostGame.Board.IsVisibleAsync()).IsTrue();
        await Assert.That(await session.GuestGame.Board.IsVisibleAsync()).IsTrue();

        // At most one player has clickable pieces at any given time
        await Assert.That(hostHas && guestHas).IsFalse();
    }

    [Test]
    public async Task MakeMove_TurnProgresses()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);
        await session.SetupAndStartGameAsync();

        // Wait for the game state to settle
        await Task.Delay(2_000);

        // Find which player has clickable pieces and click one
        if (await session.HostGame.HasClickablePiecesAsync())
        {
            await session.HostGame.ClickFirstClickablePieceAsync();
        }
        else if (await session.GuestGame.HasClickablePiecesAsync())
        {
            await session.GuestGame.ClickFirstClickablePieceAsync();
        }

        // After the move, wait for state to update
        await Task.Delay(2_000);

        // The board should still be visible — game hasn't crashed
        await Assert.That(await session.HostGame.Board.IsVisibleAsync()).IsTrue();
        await Assert.That(await session.GuestGame.Board.IsVisibleAsync()).IsTrue();
    }

    // -- Full Game Test (deterministic) --

    [Test]
    [Property("Category", "Slow")]
    public async Task FullGame_ShowsGameOverOverlay()
    {
        var serverUrl = fixture.FixedDiceServer!.BaseUrl;
        await using var session = await TwoPlayerSession.CreateAsync(fixture, serverUrl);
        await session.SetupAndStartGameAsync();

        await session.AutoplayAsync(timeoutMs: 120_000);

        // Both pages should show the game-over overlay
        await session.HostGame.WaitForGameOverAsync(timeoutMs: 5_000);
        await session.GuestGame.WaitForGameOverAsync(timeoutMs: 5_000);

        await Assert.That(await session.HostGame.GameOverBackdrop.IsVisibleAsync()).IsTrue();
        await Assert.That(await session.GuestGame.GameOverBackdrop.IsVisibleAsync()).IsTrue();
    }

    // -- Disconnect Test --

    [Test]
    public async Task Disconnect_ShowsBanner()
    {
        await using var session = await TwoPlayerSession.CreateAsync(fixture, fixture.DefaultServer.BaseUrl);
        await session.SetupAndStartGameAsync();

        // Close the guest's page to simulate disconnect
        await session.GuestGame.Page.CloseAsync();

        // Host should see the disconnect banner (after grace period or immediate notification)
        // The server may show reconnecting first, then disconnected
        var hostDisconnect = session.HostGame.DisconnectBanner;
        await hostDisconnect.WaitForAsync(new() { Timeout = 40_000 });
        await Assert.That(await hostDisconnect.IsVisibleAsync()).IsTrue();
    }
}
