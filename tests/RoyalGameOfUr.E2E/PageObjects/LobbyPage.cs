using Microsoft.Playwright;

namespace RoyalGameOfUr.E2E.PageObjects;

public sealed class LobbyPage(IPage page, string baseUrl)
{
    public IPage Page => page;

    public async Task NavigateAsync()
    {
        await page.GotoAsync($"{baseUrl}/online", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30_000
        });
        // Wait for Blazor WASM to render the lobby
        await page.WaitForSelectorAsync(".lobby", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    public async Task<string> CreateRoomAsync(string playerName, string rules = "Finkel")
    {
        await BlazorFillAsync("input[placeholder='Enter your name']", playerName);

        if (rules != "Finkel")
        {
            await page.SelectOptionAsync("select", rules);
        }

        await page.ClickAsync("button:has-text('Create Room')");

        var roomCodeEl = page.Locator(".room-code");
        await roomCodeEl.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        return (await roomCodeEl.TextContentAsync())!.Trim();
    }

    public async Task JoinRoomAsync(string playerName, string roomCode)
    {
        await BlazorFillAsync("input[placeholder='Enter your name']", playerName);
        await BlazorFillAsync("input[placeholder='ROOM CODE']", roomCode);
        await page.ClickAsync("button:has-text('Join Room')");
    }

    public async Task WaitForOpponentJoinedAsync()
    {
        await page.WaitForSelectorAsync("button:has-text('Start Game')",
            new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    public async Task StartGameAsync()
    {
        await page.ClickAsync("button:has-text('Start Game')");
    }

    public async Task WaitForGameNavigationAsync()
    {
        // Navigation may have already happened by the time we start waiting
        if (page.Url.Contains("/online/play"))
            return;

        await page.WaitForURLAsync("**/online/play",
            new PageWaitForURLOptions { Timeout = 60_000 });
    }

    public ILocator RoomCode => page.Locator(".room-code");
    public ILocator WaitingText => page.Locator(".waiting-text");
    public ILocator ErrorText => page.Locator(".error-text");
    public ILocator StartGameButton => page.Locator("button:has-text('Start Game')");

    /// <summary>
    /// Fill an input and dispatch a 'change' event so Blazor's @bind picks it up.
    /// Blazor WASM @bind uses the 'change' event (on blur), not 'input'.
    /// </summary>
    private async Task BlazorFillAsync(string selector, string value)
    {
        var locator = page.Locator(selector);
        await locator.FillAsync(value);
        await locator.DispatchEventAsync("change");
        await page.WaitForTimeoutAsync(100);
    }
}
