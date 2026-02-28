namespace RoyalGameOfUr.Engine;

public interface IGameObserver
{
    void OnStateChanged(GameState state);
    void OnDiceRolled(Player player, int roll);
    void OnMoveMade(Move move, MoveOutcome outcome);
    void OnTurnForfeited(Player player);
    void OnGameOver(Player winner);
}
