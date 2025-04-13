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
session.Map[1, 3] = TileType.Wall;
session.Map[2, 5] = TileType.Wall;
session.Map[4, 2] = TileType.Wall;

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
Bomb? placedBomb = null; // Keep track if needed, though GameManager handles list
int bombX = -1, bombY = -1; // Keep track for detonation message/check

// Helper Functions
void DoMove(int dx, int dy)
{
    actionCount++;
    var player = session.Players[player1.Id];
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

// Updated DoPlaceBomb to check return value
void DoPlaceBomb()
{
    actionCount++;
    var player = session.Players[player1.Id];
    Console.WriteLine($"\nAction {actionCount}: Try placing Bomb at ({player.X}, {player.Y}).");
    bool success = manager.PlaceBomb(player1.Id, player.X, player.Y); // Use manager, check result

    if (success)
    {
        Console.WriteLine("-> Bomb placed successfully.");
        // Only track position if needed for messages later
        bombX = player.X;
        bombY = player.Y;
    }
    else
    {
        Console.WriteLine($"-> Failed to place bomb (Limit reached or other condition). Active: {player.ActiveBombsCount}, Max: {player.MaxBombs}");
        bombX = -1; // Ensure we don't think a bomb exploded if placement failed
        bombY = -1;
    }
    DoTick();
}

void DoTick()
{
    tickCount++;
    Console.WriteLine($"--- Tick {tickCount} ---");
    manager.Tick();
    printer.PrintMap(session);
    Console.WriteLine("Press Enter for next action...");
    Console.ReadLine();
}

// Simulation Sequence
DoMove(1, 0);  // 1: R to (2,1) - Tick 1
DoMove(0, 1);  // 2: D to (2,2) - Tick 2
DoPlaceBomb(); // 3: Place Bomb at (2,2) - Tick 3 (Bomb fuse = 5)
DoMove(-1, 0); // 4: L to (1,2) - Tick 4 (Bomb fuse = 4)
DoMove(0, 1);  // 5: Try D into Wall (1,3) - Fails - Tick 5 (Bomb fuse = 3)

// Tick until bomb should explode (Total 5 ticks after placement tick)
while (session.Bombs.Any(b => b.OwnerId == player1.Id))
{
    DoTick();
}

Console.WriteLine($"\n{actionCount} Actions performed. Bomb should have exploded. Press Enter to exit.");
Console.ReadLine();