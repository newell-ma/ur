namespace RoyalGameOfUr.Engine;

public interface IGameSetup
{
    Task<GameConfiguration> ConfigureAsync(CancellationToken ct = default);
}
