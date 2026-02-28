# The Royal Game of Ur

A C# implementation of the [Royal Game of Ur](https://en.wikipedia.org/wiki/Royal_Game_of_Ur), one of the oldest known board games (~2600 BCE). Built with a UI-agnostic engine, pluggable player types, and a console host for two-player hot-seat play.

## Project Structure

```
├── src/
│   ├── RoyalGameOfUr.Engine/       # Core game logic (class library)
│   └── RoyalGameOfUr.Console/      # Console host (two-player hot-seat)
└── tests/
    └── RoyalGameOfUr.Engine.Tests/  # TUnit tests (58 tests)
```

## Getting Started

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# Build
dotnet build RoyalGameOfUr.slnx

# Run tests
dotnet run --project tests/RoyalGameOfUr.Engine.Tests/RoyalGameOfUr.Engine.Tests.csproj

# Play
dotnet run --project src/RoyalGameOfUr.Console/RoyalGameOfUr.Console.csproj
```

## How to Play

Two players take turns rolling four binary dice (0–4 range) and moving pieces along a path of 15 squares. The goal is to bear off all 7 pieces before your opponent.

```
  1: [.] [.] [.] [.] [*]      [*] [.]        * = Rosette (safe square, extra turn)
                    [.] [.] [.] [*] [.] [.] [.] [.]    Middle row = shared lane
  2: [.] [.] [.] [.] [*]      [*] [.]        1/2 = player pieces
```

**Key rules:**
- **Rosettes** (marked `*`) grant an extra turn and are safe from capture
- **Captures** — land on an opponent's piece in the shared lane to send it back to start
- **Exact bear-off** — you must roll the exact number to move a piece off the board
- **Forfeit** — if you have no valid moves, your turn is skipped

## Engine Architecture

The engine is designed to be UI-agnostic and extensible:

- **`Game`** — pure rule engine enforcing Roll → Move/Forfeit protocol
- **`GameRules`** — immutable configuration with named presets (`Finkel`, `Simple`)
- **`GameState`** / **`GameStateBuilder`** — board state with fluent builder for tests and scenarios
- **`IPlayer`** — async interface for pluggable player types (human, AI, network)
- **`GameRunner`** — game loop orchestrator with events (`OnDiceRolled`, `OnMoveMade`, `OnGameOver`, etc.)
- **`IDice`** — dice abstraction for testability

### Adding a Custom Player

Implement `IPlayer` and pass it to `GameRunner`:

```csharp
public class MyAiPlayer : IPlayer
{
    public string Name => "AI";

    public Task<Move> ChooseMoveAsync(GameState state, IReadOnlyList<Move> validMoves, int roll)
    {
        // Pick the best move
        return Task.FromResult(validMoves[0]);
    }
}
```

### Configurable Rules

```csharp
// Use a preset
var rules = GameRules.Finkel;   // Rosettes at 4, 8, 14 — 7 pieces
var rules = GameRules.Simple;   // Rosettes at 4, 8 — 7 pieces

// Or define custom rules
var rules = new GameRules(
    rosettePositions: new HashSet<int> { 4, 8, 14 },
    piecesPerPlayer: 7,
    pathLength: 15,
    sharedLaneStart: 5,
    sharedLaneEnd: 12);
```

## Tests

58 TUnit tests covering dice, rules, move generation, move execution, captures, rosettes, bear-off, forfeits, and full integration scenarios.

```
Test run summary: Passed!
  total: 58
  failed: 0
  succeeded: 58
```
