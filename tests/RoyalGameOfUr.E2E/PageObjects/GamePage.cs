using Microsoft.Playwright;

namespace RoyalGameOfUr.E2E.PageObjects;

public sealed class GamePage(IPage page)
{
    public IPage Page => page;

    public async Task WaitForBoardAsync()
    {
        await page.WaitForSelectorAsync("svg.board-svg",
            new PageWaitForSelectorOptions { Timeout = 15_000 });
    }

    public async Task ClickFirstClickablePieceAsync()
    {
        // Prefer start pieces (entering the board) then board pieces
        var startPiece = page.Locator(".start-piece.clickable").First;
        var boardPiece = page.Locator("g.piece.clickable").First;

        if (await startPiece.IsVisibleAsync())
        {
            await startPiece.ClickAsync();
        }
        else
        {
            await boardPiece.ClickAsync();
        }
    }

    public async Task<bool> HasClickablePiecesAsync()
    {
        var count = await page.Locator(".start-piece.clickable, g.piece.clickable").CountAsync();
        return count > 0;
    }

    public async Task WaitForGameOverAsync(float timeoutMs = 120_000)
    {
        await page.WaitForSelectorAsync(".game-over-backdrop",
            new PageWaitForSelectorOptions { Timeout = timeoutMs });
    }

    public ILocator Board => page.Locator("svg.board-svg");
    public ILocator DiceContainer => page.Locator(".dice-container");
    public ILocator InfoBar => page.Locator(".online-info-bar");
    public ILocator DisconnectBanner => page.Locator(".disconnect-banner");
    public ILocator StatusMessage => page.Locator(".status-message");
    public ILocator GameOverBackdrop => page.Locator(".game-over-backdrop");
    public ILocator WaitingTurn => page.Locator(".waiting-turn");
    public ILocator ClickablePieces => page.Locator(".start-piece.clickable, g.piece.clickable");
}
