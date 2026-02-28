namespace RoyalGameOfUr.Engine;

public interface IGameObserver
{
    Task OnStateChangedAsync(GameState state);
    Task OnDiceRolledAsync(Player player, int roll);
    Task OnMoveMadeAsync(Move move, MoveOutcome outcome);
    Task OnTurnForfeitedAsync(Player player);
    Task OnGameOverAsync(Player winner);
}
