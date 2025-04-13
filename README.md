# Bomberman Backend

A C# backend engine for a Bomberman-style game, designed for integration into a larger system (e.g., using a message bus).

## Current Features

This project currently handles:

- **Game State:** Managed via `GameSession` holding the map, players, and active bombs.
- **Game Logic:** Orchestrated by `GameManager`, handling player actions and game updates.
- **Grid-Based Map:** Uses a `TileType[,]` array supporting `Empty`, `Wall` (indestructible), `DestructibleWall`, `Bomb`, and `Player` tiles.
- **Player Movement:** Players can move cardinally, blocked by `Wall` and `DestructibleWall` types. Logic allows moving onto `Bomb` tiles.
- **Bomb Mechanics:**
  - Players can place bombs at their feet, respecting a `MaxBombs` limit per player (`Player.ActiveBombsCount`).
  - Bombs have a timed fuse (`RemainingFuseTicks`), managed centrally via `GameManager.Tick()`.
  - Basic explosion logic implemented in `DetonateBomb`:
    - Calculates blast area based on `BlastRadius`.
    - Blast propagation stops at `Wall` and `DestructibleWall` tiles.
    - Clears `Empty` and destroys `DestructibleWall` tiles within the blast.
    - Removes players hit by the blast.
    - Handles basic chain reactions for bombs hit by an explosion within the same tick.
- **Unit Tests:** Uses xUnit for testing core game logic (movement, bomb placement/timing/detonation).

## Structure

Located within the `Mono Repo` folder:

- `BombermanBackend/` — Core game logic (`Logic/`) and models (`Models/`). Also includes the `Program.cs` simulation driver.
- `BombermanBackend.Tests/` — Unit tests for gameplay mechanics.

## Usage

### Running the Simulation

To run the current console simulation driver:

```bash
dotnet run --project BombermanBackend
dotnet test
```

## Next Steps / TODOs

- Implement detailed explosion effects (e.g., temporary blast tiles, damage calculation).
- Add different power-ups (e.g., increase `MaxBombs`, `BlastRadius`, movement speed).
- Implement player lives and respawn mechanics.
- Refine chain reaction logic if needed.
- Integrate with message bus for handling commands and broadcasting state changes.
- Add comprehensive unit tests for explosion scenarios.
- Improve map representation if needed (e.g., multiple items per tile).
- Implement map generation logic instead of hardcoding in `Program.cs`.
