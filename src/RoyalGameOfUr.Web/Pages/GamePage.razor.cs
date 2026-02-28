using Microsoft.AspNetCore.Components;
using RoyalGameOfUr.Engine;
using RoyalGameOfUr.Web.Services;

namespace RoyalGameOfUr.Web.Pages;

public partial class GamePage : IDisposable
{
    private BoardCoordinateMapper? _mapper;
    private string _selectedRules = "Finkel";
    private string _selectedMode = "HumanVsAI";
    private bool _diceRolling;

    protected override async Task OnInitializedAsync()
    {
        SubscribeEvents();
        _mapper = new BoardCoordinateMapper(GameSvc.Rules);
        await StartNewGame();
    }

    private void SubscribeEvents()
    {
        GameSvc.OnStateChanged += HandleStateChanged;
        GameSvc.OnDiceRolledEvent += HandleDiceRolled;
        GameSvc.OnMoveRequested += HandleMoveRequested;
        GameSvc.OnSkipRequested += HandleSkipRequested;
        GameSvc.OnGameOverEvent += HandleGameOver;
    }

    private void UnsubscribeEvents()
    {
        GameSvc.OnStateChanged -= HandleStateChanged;
        GameSvc.OnDiceRolledEvent -= HandleDiceRolled;
        GameSvc.OnMoveRequested -= HandleMoveRequested;
        GameSvc.OnSkipRequested -= HandleSkipRequested;
        GameSvc.OnGameOverEvent -= HandleGameOver;
    }

    private async Task StartNewGame()
    {
        var rules = _selectedRules switch
        {
            "Simple" => GameRules.Simple,
            "Masters" => GameRules.Masters,
            "Blitz" => GameRules.Blitz,
            "Tournament" => GameRules.Tournament,
            _ => GameRules.Finkel
        };

        _mapper = new BoardCoordinateMapper(rules);

        var (p1Type, p1Name, p2Type, p2Name) = _selectedMode switch
        {
            "HotSeat" => (PlayerType.Human, "Player 1", PlayerType.Human, "Player 2"),
            "AIDemo" => (PlayerType.Computer, "AI One", PlayerType.Computer, "AI Two"),
            _ => (PlayerType.Human, "Player", PlayerType.Computer, "Shamhat")
        };

        await GameSvc.StartGameAsync(rules, p1Type, p1Name, p2Type, p2Name);
    }

    private async Task HandleStateChanged()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleDiceRolled()
    {
        _diceRolling = true;
        await InvokeAsync(StateHasChanged);

        // Let dice animation play
        await Task.Delay(900);
        _diceRolling = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleMoveRequested()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleSkipRequested()
    {
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleGameOver()
    {
        await InvokeAsync(StateHasChanged);
    }

    private void HandleMoveSelected(Move move)
    {
        GameSvc.SubmitMove(move);
    }

    public void Dispose()
    {
        UnsubscribeEvents();
        GameSvc.StopGame();
    }
}
