using Microsoft.Playwright;
using RoyalGameOfUr.E2E.Infrastructure;
using RoyalGameOfUr.E2E.PageObjects;

namespace RoyalGameOfUr.E2E.Helpers;

public sealed class TwoPlayerSession : IAsyncDisposable
{
    private IBrowserContext? _hostContext;
    private IBrowserContext? _guestContext;

    public LobbyPage HostLobby { get; private set; } = null!;
    public LobbyPage GuestLobby { get; private set; } = null!;
    public GamePage HostGame { get; private set; } = null!;
    public GamePage GuestGame { get; private set; } = null!;
    public string RoomCode { get; private set; } = "";

    public static async Task<TwoPlayerSession> CreateAsync(AppFixture fixture, string baseUrl)
    {
        var session = new TwoPlayerSession();
        session._hostContext = await fixture.NewContextAsync();
        session._guestContext = await fixture.NewContextAsync();

        await session._hostContext.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true, Snapshots = true, Sources = false
        });
        await session._guestContext.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true, Snapshots = true, Sources = false
        });

        var hostPage = await session._hostContext.NewPageAsync();
        var guestPage = await session._guestContext.NewPageAsync();

        session.HostLobby = new LobbyPage(hostPage, baseUrl);
        session.GuestLobby = new LobbyPage(guestPage, baseUrl);
        session.HostGame = new GamePage(hostPage);
        session.GuestGame = new GamePage(guestPage);

        return session;
    }

    public async Task SaveArtifactsAsync(string testName)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
        Directory.CreateDirectory(dir);

        try
        {
            await HostLobby.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(dir, $"{testName}-host.png")
            });
        }
        catch { /* page may be closed */ }

        try
        {
            await GuestLobby.Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(dir, $"{testName}-guest.png")
            });
        }
        catch { /* page may be closed */ }

        if (_hostContext is not null)
            await _hostContext.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(dir, $"{testName}-host.zip")
            });

        if (_guestContext is not null)
            await _guestContext.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(dir, $"{testName}-guest.zip")
            });
    }

    public async Task SetupAndStartGameAsync(
        string hostName = "Alice",
        string guestName = "Bob",
        string rules = "Finkel")
    {
        // Host creates room
        await HostLobby.NavigateAsync();
        RoomCode = await HostLobby.CreateRoomAsync(hostName, rules);

        // Guest joins room
        await GuestLobby.NavigateAsync();
        await GuestLobby.JoinRoomAsync(guestName, RoomCode);

        // Host sees opponent joined and starts game
        await HostLobby.WaitForOpponentJoinedAsync();
        await HostLobby.StartGameAsync();

        // Both navigate to game page
        await Task.WhenAll(
            HostLobby.WaitForGameNavigationAsync(),
            GuestLobby.WaitForGameNavigationAsync());

        // Both see the board
        await Task.WhenAll(
            HostGame.WaitForBoardAsync(),
            GuestGame.WaitForBoardAsync());
    }

    /// <summary>
    /// Plays the game to completion by clicking clickable pieces on whichever
    /// player's page has them. Returns when the game-over overlay appears.
    /// </summary>
    public async Task AutoplayAsync(float timeoutMs = 120_000)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        while (!cts.Token.IsCancellationRequested)
        {
            // Check if game is over on either page
            if (await GameOverVisibleAsync(HostGame) || await GameOverVisibleAsync(GuestGame))
                return;

            // Try to click a piece on the host's page
            if (await TryClickPieceAsync(HostGame))
            {
                await Task.Delay(150, cts.Token);
                continue;
            }

            // Try to click a piece on the guest's page
            if (await TryClickPieceAsync(GuestGame))
            {
                await Task.Delay(150, cts.Token);
                continue;
            }

            // Neither player has clickable pieces — wait briefly for state update
            await Task.Delay(200, cts.Token);
        }

        throw new TimeoutException("Autoplay timed out before game over.");
    }

    private static async Task<bool> TryClickPieceAsync(GamePage gamePage)
    {
        try
        {
            if (await gamePage.HasClickablePiecesAsync())
            {
                await gamePage.ClickFirstClickablePieceAsync();
                return true;
            }
        }
        catch (PlaywrightException)
        {
            // Element may have become stale between check and click — retry next loop
        }

        return false;
    }

    private static async Task<bool> GameOverVisibleAsync(GamePage gamePage)
    {
        return await gamePage.GameOverBackdrop.IsVisibleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hostContext is not null) await _hostContext.DisposeAsync();
        if (_guestContext is not null) await _guestContext.DisposeAsync();
    }
}
