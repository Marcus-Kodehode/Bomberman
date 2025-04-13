using BombermanBackend.Models;
using BombermanBackend.Logic;
using System;
using System.Linq;
using System.Text;

// --- Simulation State Variables ---
int actionCount = 0;
int tickCount = 0;

// --- Helper Functions (No changes needed here from previous version) ---

// Helper to attempt a move and report concisely
void TryMove(GameManager manager, GameSession session, MapPrinter printer, Player player, int dx, int dy, string description)
{
    actionCount++;
    int targetX = player.X + dx;
    int targetY = player.Y + dy;
    Console.WriteLine($"\nAction {actionCount}: P{player.Id} attempts {description} to ({targetX}, {targetY})...");
    bool success = manager.MovePlayer(player.Id, targetX, targetY); // Use manager's MovePlayer
    if (success)
    {
        Console.WriteLine($" -> Success. New pos: ({player.X}, {player.Y})");
        // Check if a power-up was just collected (logic is in GameSession.MovePlayer)
        // We could add a check here based on player stats if needed for simulation output
        // E.g., if (player.MaxBombs > initialMaxBombs) Console.WriteLine("   (Collected Bomb PowerUp!)");
    }
    else
    {
        string reason = "Out of bounds";
        if (targetX >= 0 && targetX < session.Map.GetLength(0) && targetY >= 0 && targetY < session.Map.GetLength(1))
        {
            reason = $"Blocked by {session.Map[targetX, targetY]}"; // Use session to check map tile
        }
        Console.WriteLine($" -> Failed ({reason}). Pos unchanged: ({player.X}, {player.Y})");
    }
    DoTick(manager, session, printer); // Always tick after an action attempt
}

// Helper to place bomb and report concisely
void TryPlaceBomb(GameManager manager, GameSession session, MapPrinter printer, Player player)
{
    actionCount++;
    Console.WriteLine($"\nAction {actionCount}: P{player.Id} attempts placing Bomb at ({player.X}, {player.Y})...");
    bool success = manager.PlaceBomb(player.Id, player.X, player.Y); // Use manager's PlaceBomb
    if (success)
    {
        Console.WriteLine($" -> Success. (Active: {player.ActiveBombsCount}/{player.MaxBombs})");
    }
    else
    {
        // Add reason for failure (e.g., bomb limit reached)
        Console.WriteLine($" -> Failed (Limit Reached?). (Active: {player.ActiveBombsCount}/{player.MaxBombs})");
    }
    DoTick(manager, session, printer); // Always tick after an action attempt
}

// Tick helper (no changes needed here)
void DoTick(GameManager manager, GameSession session, MapPrinter printer, bool waitForKey = true)
{
    tickCount++;
    Console.WriteLine($"--- Tick {tickCount} ---");
    manager.Tick();
    printer.PrintMap(session);
    StringBuilder stats = new StringBuilder(">> Stats: ");
    var players = session.Players.Values;
    if (!players.Any()) { stats.Append("No players left."); }
    else { stats.AppendJoin(" | ", players.OrderBy(p => p.Id).Select(p => $"P{p.Id}({p.X},{p.Y}) R={p.BlastRadius} B={p.ActiveBombsCount}/{p.MaxBombs}")); }
    Console.WriteLine(stats.ToString());
    if (waitForKey) { Console.WriteLine("Press Enter for next action/tick..."); Console.ReadLine(); }
    else { System.Threading.Thread.Sleep(300); }
}

// --- Simulation Setup (No changes needed here) ---

const int mapWidth = 7;
const int mapHeight = 7;
var session = new GameSession(mapWidth, mapHeight);
var manager = new GameManager(session);
var printer = new MapPrinter();
for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++) session.Map[x, y] = TileType.Empty;
for (int x = 0; x < mapWidth; x++) { session.Map[x, 0] = TileType.Wall; session.Map[x, mapHeight - 1] = TileType.Wall; }
for (int y = 1; y < mapHeight - 1; y++) { session.Map[0, y] = TileType.Wall; session.Map[mapWidth - 1, y] = TileType.Wall; }
session.Map[3, 3] = TileType.Wall; session.Map[3, 4] = TileType.Wall; session.Map[1, 3] = TileType.DestructibleWall;
session.Map[2, 5] = TileType.Wall; session.Map[4, 2] = TileType.Wall; session.Map[3, 2] = TileType.DestructibleWall;
session.Map[4, 4] = TileType.DestructibleWall; session.Map[5, 3] = TileType.DestructibleWall;
session.Map[1, 2] = TileType.PowerUpBlastRadius; // 'F'
session.Map[1, 4] = TileType.PowerUpBombCount;   // '+'
var player1 = new Player { Id = "1", X = 1, Y = 1 };
var player2 = new Player { Id = "2", X = mapWidth - 2, Y = mapHeight - 2 };
session.AddPlayer(player1);
session.AddPlayer(player2);
Console.Clear();
Console.WriteLine($"Initial State ({mapWidth}x{mapHeight}):");
printer.PrintMap(session);
Console.WriteLine($">> P1 Initial: R={player1.BlastRadius} B={player1.ActiveBombsCount}/{player1.MaxBombs}");
Console.WriteLine($">> P2 Initial: R={player2.BlastRadius} B={player2.ActiveBombsCount}/{player2.MaxBombs}");
Console.WriteLine("Press Enter to start simulation...");
Console.ReadLine();


// --- Simulation Sequence (Corrected P1 Path to collect PowerUp) ---
TryMove(manager, session, printer, player1, 0, 1, "move Down");  // 1: P1 (1,1)->(1,2). Collect 'F'. R=2. Tick 1.
TryMove(manager, session, printer, player2, -1, 0, "move Left"); // 2: P2 (5,5)->(4,5). Tick 2.
TryMove(manager, session, printer, player1, 0, 1, "move Down");  // 3: P1 (1,2)->(1,3). Hit DestructibleWall 'X'. Failed. Tick 3. P1 still at (1,2).
TryMove(manager, session, printer, player2, 0, -1, "move Up");   // 4: P2 (4,5)->(4,4). Hit DestructibleWall 'X'. Failed. Tick 4. P2 still at (4,5).
TryMove(manager, session, printer, player1, 1, 0, "move Right"); // 5: P1 (1,2)->(2,2). Success. Tick 5.
TryMove(manager, session, printer, player2, -1, 0, "move Left"); // 6: P2 (4,5)->(3,5). Success. Tick 6.
TryMove(manager, session, printer, player1, 0, 1, "move Down");  // 7: P1 (2,2)->(2,3). Success. Tick 7.
TryMove(manager, session, printer, player1, -1, 0, "move Left"); // 8: P1 (2,3)->(1,3). Hit DestructibleWall 'X'. Failed. Tick 8. P1 still at (2,3).
// --- Path Correction Start ---
TryMove(manager, session, printer, player1, 0, 1, "move Down");  // 9: P1 (2,3)->(2,4). Success. Tick 9.
TryMove(manager, session, printer, player1, -1, 0, "move Left"); // 10: P1 (2,4)->(1,4). Collect '+'. MaxB=2. Tick 10.
// --- Path Correction End ---
TryPlaceBomb(manager, session, printer, player1);                // 11: P1 places Bomb 1 at (1,4). Success. Active=1/2. Tick 11.
TryMove(manager, session, printer, player1, 1, 0, "move Right"); // 12: P1 (1,4)->(2,4). Success. Tick 12.
TryPlaceBomb(manager, session, printer, player1);                // 13: P1 places Bomb 2 at (2,4). Success. Active=2/2. Tick 13.
TryMove(manager, session, printer, player1, 0, -1, "move Up");   // 14: P1 (2,4)->(2,3). Success. Tick 14. (Bombs ticking down)


// Comment updated to reflect the actual actions
Console.WriteLine("\nP1 collected powerup, placed two bombs & moved away. Continuing ticks automatically...");

// Helper function to check if bombs or explosions exist using the session
bool GameHasActiveEffects(GameSession gameSession)
{
    if (gameSession.Bombs.Any()) return true;
    int width = gameSession.Map.GetLength(0);
    int height = gameSession.Map.GetLength(1);
    for (int x = 0; x < width; x++) { for (int y = 0; y < height; y++) { if (gameSession.Map[x, y] == TileType.Explosion) return true; } }
    return false;
}

// Let ticks run automatically until bombs/explosions are gone
while (GameHasActiveEffects(session))
{
    DoTick(manager, session, printer, false); // Tick without waiting
}
Console.WriteLine("Final cleanup tick...");
DoTick(manager, session, printer, true); // Final tick with pause

Console.WriteLine($"\n{actionCount} Actions performed, {tickCount} Ticks simulated. Press Enter to exit.");
Console.ReadLine();