# Bomberman Backend

A C# backend engine for a Bomberman-style game, designed for integration into a larger system (e.g., using a message bus).

## Current Features

This project currently handles:

- **Game State:** Managed via `GameSession` holding the map, players, and active bombs.
- **Game Logic:** Orchestrated by `GameManager`, handling player actions and game updates.
- **Grid-Based Map:** Uses a `TileType[,]` array supporting `Empty`, `Wall` (indestructible), `DestructibleWall`, `Bomb`, `Player`, `Explosion`, and Power-up tiles.
- **Player Movement:** Players can move cardinally, blocked by `Wall` and `DestructibleWall` types. Logic allows moving onto `Bomb` and `PowerUp` tiles.
- **Bomb Mechanics:**
  - Players can place bombs at their feet, respecting a `MaxBombs` limit per player.
  - Bombs have a timed fuse (`RemainingFuseTicks`), managed centrally via `GameManager.Tick()`.
  - Basic explosion logic implemented in `GameManager`, handling blast radius, wall interactions, player removal, and chain reactions.
- **Power-ups:** Supports increasing `MaxBombs` and `BlastRadius` via collectible items on the map.
- **Unit Tests:** Uses xUnit for testing core game logic.

## Structure

Located within the `Mono Repo` folder:

- `BombermanBackend/` — Core game logic (`Logic/`) and models (`Models/`).
- `BombermanBackend.Tests/` — Unit tests for gameplay mechanics.

## Message Bus Communication (Frontend <-> Backend)

This section outlines the expected message flow when integrating with a message bus.

### Commands (Frontend -> Backend)

The frontend should send command messages to the backend to trigger player actions or game setup actions. The backend will listen for these message types.

**1. Join Game**

- **Purpose:** Request for a player to join the game session.
- **Message Type (Example):** `JoinGameCommand`
- **Properties:**
  ```csharp
  public class JoinGameCommand
  {
      public string DesiredPlayerId { get; set; } // Suggest an ID for the player
      // Potentially add starting position preferences later
  }
  ```
- **Backend Action:** Corresponds roughly to logic within `GameSession.AddPlayer`. The backend might assign the final ID and starting position.

**2. Move Player**

- **Purpose:** Request to move a specific player one step cardinally.
- **Message Type (Example):** `PlayerMoveCommand`
- **Properties:**
  ```csharp
  public class PlayerMoveCommand
  {
      public string PlayerId { get; set; } // ID of the player moving
      public int Dx { get; set; } // Change in X (-1, 0, or 1)
      public int Dy { get; set; } // Change in Y (-1, 0, or 1), ensure Dx+Dy magnitude is 1
  }
  ```
- **Backend Action:** Corresponds to `GameManager.MovePlayer`.

**3. Place Bomb**

- **Purpose:** Request for a player to place a bomb at their current location.
- **Message Type (Example):** `PlaceBombCommand`
- **Properties:**
  ```csharp
  public class PlaceBombCommand
  {
      public string PlayerId { get; set; } // ID of the player placing the bomb
  }
  ```
- **Backend Action:** Corresponds to `GameManager.PlaceBomb`.

_(Note: These are example DTO structures. The exact fields and potentially a shared library for these contracts should be defined based on the chosen message bus technology and serialization format (e.g., JSON).)_

### Events / State Updates (Backend -> Frontend)

The backend will publish messages to notify the frontend about changes in the game state. The frontend should subscribe to these messages to update the UI.

**Common Messages:**

- **`GameStateUpdate`:** A snapshot of the current game state (player positions/stats, bomb locations/fuses, map layout). This might be sent periodically or after significant changes.
- **`PlayerJoinedEvent`:** Confirms a player successfully joined.
- **`PlayerDiedEvent`:** Indicates a player was removed (e.g., by an explosion).
- **`BombExplodedEvent`:** Details about a bomb explosion, including affected tiles.
- **`PowerUpCollectedEvent`:** Indicates a player collected a power-up.
- **`GameOverEvent`:** Signals the end of the game, potentially indicating the winner.

_(The exact granularity and frequency of these events depend on the frontend's requirements for smooth rendering and responsiveness.)_

## Usage

### Running the Simulation

To run the current console simulation driver (useful for testing core logic):

```bash
Dotnet run --project BombermanBackend
cd BombermanBackend.Tests
dotnet test
```

## Next Steps / TODOs

1.  **Define Message Contracts (DTOs):** Create the actual Command/Event DTO classes, ideally in a shared library project.
2.  **Choose Message Bus:** Select a specific message bus technology (e.g., RabbitMQ, NATS, Azure Service Bus) based on project needs.
3.  **Implement Bus Client Library:** Add the chosen message bus client library (NuGet package) to the backend project.
4.  **Build Communication Layer:** Create the backend service layer/components responsible for:
    - Connecting to the message bus.
    - Handling incoming command DTOs (subscribing to messages, deserializing, calling `GameManager`).
    - Publishing event/state DTOs based on `GameManager` outcomes.
5.  **Adapt `Program.cs`:** Replace or modify `Program.cs` to configure and run the backend as a persistent service that initializes the communication layer and listens to the message bus, instead of running the simulation.
6.  **Add Integration Tests:** Create comprehensive unit and integration tests specifically for the message handling layer and bus communication.
7.  **Improve Map Representation (Optional):** If needed, enhance the map model (e.g., `GameSession.Map`) to potentially allow multiple items (like a player _and_ a power-up, or a player _and_ a bomb) on the same tile if current logic doesn't support it sufficiently.
8.  **Implement Map Generation:** Replace the hardcoded map layout in `Program.cs` (or wherever it ends up) with dynamic map generation logic.

Sources and related content
