using BombermanBackend.Models;
using BombermanBackend.Logic;
using System;
using System.Linq;

const int mapWidth = 7;
const int mapHeight = 7;

var session = new GameSession(mapWidth, mapHeight);
var manager = new GameManager(session);
var printer = new MapPrinter();

// Initialize Map
for (int x = 0; x < session.Map.GetLength(0); x++)
    for (int y = 0; y < session.Map.GetLength(1); y++)
        session.Map[x, y] = TileType.Empty;

for (int x = 0; x < session.Map.GetLength(0); x++)
{
    session.Map[x, 0] = TileType.Wall;
    session.Map[x, session.Map.GetLength(1) - 1] = TileType.Wall;
}
for (int y = 1; y < session.Map.GetLength(1) - 1; y++)
{
    session.Map[0, y] = TileType.Wall;
    session.Map[session.Map.GetLength(0) - 1, y] = TileType.Wall;
}
session.Map[3, 3] = TileType.Wall;
session.Map[3, 4] = TileType.Wall;
session.Map[1, 3] = TileType.DestructibleWall; // Changed for testing explosion
session.Map[2, 5] = TileType.Wall;
session.Map[4, 2] = TileType.Wall;
session.Map[3, 2] = TileType.DestructibleWall;


// Simulation Start
var player1 = new Player { Id = "player1", X = 1, Y = 1 };
session.AddPlayer(player1);

Console.Clear();
Console.WriteLine($"Initial State ({mapWidth}x{mapHeight}):");
printer.PrintMap(session);
Console.WriteLine("Press Enter to advance simulation step-by-step...");
Console.ReadLine();

int actionCount = 0;
int tickCount = 0;

// Helper Functions (Unchanged)
void DoMove(int dx, int dy)
{
    actionCount++;
    var player = session.Players.GetValueOrDefault(player1.Id); // Use GetValueOrDefault for safety after player removal
    if (player == null)
    {
        Console.WriteLine($"\nAction {actionCount}: Player {player1.Id} not found, skipping move.");
        DoTick(); // Still advance time even if player is gone
        return;
    }
    int currentX = player.X;
    int currentY = player.Y;
    int targetX = currentX + dx;
    int targetY = currentY + dy;

    Console.WriteLine($"\nAction {actionCount}: Try move to ({targetX}, {targetY})");
    bool success = manager.MovePlayer(player1.Id, targetX, targetY);
    if (!success)
    {
        if (targetX >= 0 && targetX < session.Map.GetLength(0) && targetY >= 0 && targetY < session.Map.GetLength(1))
        {
            Console.WriteLine($"-> Blocked by {session.Map[targetX, targetY]}. Pos: ({player.X}, {player.Y})");
        }
        else
        {
            Console.WriteLine($"-> Out of bounds.");
        }
    }
    else
    {
        Console.WriteLine($"-> Success. New pos: ({player.X}, {player.Y})");
    }
    DoTick();
}

void DoPlaceBomb()
{
    actionCount++;
    var player = session.Players.GetValueOrDefault(player1.Id);
    if (player == null)
    {
        Console.WriteLine($"\nAction {actionCount}: Player {player1.Id} not found, skipping bomb place.");
        DoTick(); // Still advance time
        return;
    }

    Console.WriteLine($"\nAction {actionCount}: Try placing Bomb at ({player.X}, {player.Y}).");
    bool success = manager.PlaceBomb(player1.Id, player.X, player.Y);

    if (success)
    {
        Console.WriteLine("-> Bomb placed successfully.");
    }
    else
    {
        Console.WriteLine($"-> Failed to place bomb (Limit reached or other condition). Active: {player.ActiveBombsCount}, Max: {player.MaxBombs}");
    }
    DoTick();
}

void DoTick()
{
    tickCount++;
    Console.WriteLine($"--- Tick {tickCount} ---");
    manager.Tick(); // Cleanup happens here, then UpdateBombs/Detonate
    printer.PrintMap(session);
    Console.WriteLine("Press Enter for next action/tick...");
    Console.ReadLine();
}

// Simulation Sequence - 5 Actions, then Ticks until 10 total
DoMove(1, 0);  // 1: R to (2,1) - Tick 1
DoMove(0, 1);  // 2: D to (2,2) - Tick 2
DoMove(0, 1);  // 3: D to (2,3) - Tick 3
DoPlaceBomb(); // 4: Place Bomb at (2,3) - Tick 4 (Bomb fuse = 5 -> 4)
DoMove(0, -1); // 5: U to (2,2) - Tick 5 (Bomb fuse = 4 -> 3)

Console.WriteLine("\nPlayer moved away. Continuing ticks...");

// --- MODIFICATION START: Explicit Ticks 6-10 ---
if (tickCount < 6) DoTick(); // Tick 6 (Fuse 2)
if (tickCount < 7) DoTick(); // Tick 7 (Fuse 1)
if (tickCount < 8) DoTick(); // Tick 8 (Fuse 0 -> BOOM, '*' appears)
if (tickCount < 9) DoTick(); // Tick 9 ('*' cleared by CleanupExplosions)
if (tickCount < 10) DoTick(); // Tick 10 (Stable empty state)
// --- MODIFICATION END ---


Console.WriteLine($"\n{actionCount} Actions performed, {tickCount} Ticks simulated. Press Enter to exit.");
Console.ReadLine();