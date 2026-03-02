using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Server.Rooms;
using TUnit.Core.Interfaces;

namespace RoyalGameOfUr.E2E.Infrastructure;

public sealed class AppFixture : IAsyncInitializer, IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public TestServerHost DefaultServer { get; private set; } = null!;
    public TestServerHost? FixedDiceServer { get; private set; }

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        var channel = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSER_CHANNEL") ?? "msedge";
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = channel == "" ? null : channel,
            Headless = true
        });

        // Start default server (normal dice)
        DefaultServer = new TestServerHost();
        await DefaultServer.StartAsync();

        // Start fixed-dice server (for deterministic full-game tests)
        FixedDiceServer = new TestServerHost(services =>
        {
            // Replace the RoomManager with one that uses a cycling [3,0] dice.
            // P1 always rolls 3 (advance), P2 always rolls 0 (forfeit).
            // With Finkel rules, roll=3 lands on positions (2,5,8,11,14=borne off)
            // which never hit rosettes (3,7,13), so no extra-turn complications.
            services.AddSingleton(new RoomManager(
                diceFactory: _ => new CyclingDice([3, 0])));
        });
        await FixedDiceServer.StartAsync();

        // Warm up: load Blazor WASM once so assemblies are cached
        await using var warmupCtx = await _browser.NewContextAsync();
        var warmupPage = await warmupCtx.NewPageAsync();
        await warmupPage.GotoAsync(DefaultServer.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        await warmupPage.WaitForSelectorAsync("h1", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    public Task<IBrowserContext> NewContextAsync()
    {
        return _browser!.NewContextAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (FixedDiceServer is not null) await FixedDiceServer.DisposeAsync();
        await DefaultServer.DisposeAsync();

        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}

public sealed class CyclingDice(int[] values) : IDice
{
    private int _index;

    public int Roll() => values[_index++ % values.Length];
}
