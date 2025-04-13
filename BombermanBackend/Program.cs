using BombermanBackend.Models;
using BombermanBackend.Logic;
using System;
using System.Linq;
using System.Text;

// --- Define Helper Functions First (Using ONLY parameters) ---
int actionCount = 0;
int tickCount = 0;

void DoMove(GameManager manager, GameSession session, MapPrinter printer, Player player, int dx, int dy)
{
    // Ensure player object passed is valid and exists in session
    if (player == null || !session.Players.ContainsKey(player.Id))
    {
        Console.WriteLine($"\nAction {actionCount + 1}: Player {(player?.Id ?? "??")} not valid or not found, skipping move.");
        actionCount++; // Still count action attempt
        DoTick(manager, session, printer, null, true); // Tick time, pass null for player context
        return;
    }

    actionCount++;
    int currentX = player.X; int currentY = player.Y;
    int targetX = currentX + dx; int targetY = currentY + dy;
    Console.WriteLine($"\nAction {actionCount}: Player '{player.Id}' try move to ({targetX}, {targetY})");
    bool success = manager.MovePlayer(player.Id, targetX, targetY); // Use manager parameter
    if (!success)
    {
        if (targetX >= 0 && targetX < session.Map.GetLength(0) && targetY >= 0 && targetY < session.Map.GetLength(1))
        {
            Console.WriteLine($"-> Blocked by {session.Map[targetX, targetY]}. Pos: ({player.X}, {player.Y})"); // Use session parameter
        }
        else { Console.WriteLine($"-> Out of bounds."); }
    }
    else { Console.WriteLine($"-> Success. New pos: ({player.X}, {player.Y})"); }
    DoTick(manager, session, printer, player); // Pass objects
}

void DoPlaceBomb(GameManager manager, GameSession session, MapPrinter printer, Player player)
{
    if (player == null || !session.Players.ContainsKey(player.Id))
    {
        Console.WriteLine($"\nAction {actionCount + 1}: Player {(player?.Id ?? "??")} not found, skipping bomb place.");
        actionCount++;
        DoTick(manager, session, printer, null, true);
        return;
    }

    actionCount++;
    Console.WriteLine($"\nAction {actionCount}: Player '{player.Id}' try placing Bomb at ({player.X}, {player.Y}).");
    bool success = manager.PlaceBomb(player.Id, player.X, player.Y); // Use manager parameter
    if (success) { Console.WriteLine("-> Bomb placed successfully."); }
    else { Console.WriteLine($"-> Failed to place bomb (Limit reached?). Active: {player.ActiveBombsCount}, Max: {player.MaxBombs}"); }
    DoTick(manager, session, printer, player); // Pass objects
}

// DoTick now takes all objects as parameters
void DoTick(GameManager manager, GameSession session, MapPrinter printer, Player? playerContext = null, bool waitForKey = true)
{
    tickCount++;
    Console.WriteLine($"--- Tick {tickCount} ---");
    manager.Tick(); // Use passed manager
    printer.PrintMap(session); // Use passed printer/session

    // Display stats for ALL current players in the session
    StringBuilder stats = new StringBuilder(">> Post-Tick Stats: ");
    if (!session.Players.Any())
    { // Use passed session
        stats.Append("No players remaining.");
    }
    else
    {
        foreach (var p in session.Players.Values.OrderBy(p => p.Id))
        {
            stats.Append($"P{p.Id}({p.X},{p.Y}) R={p.BlastRadius}, MaxB={p.MaxBombs}, ActiveB={p.ActiveBombsCount} | ");
        }
    }
    Console.WriteLine(stats.ToString().TrimEnd(' ', '|'));

    if (waitForKey)
    {
        Console.WriteLine("Press Enter for next action/tick...");
        Console.ReadLine();
    }
    else
    {
        System.Threading.Thread.Sleep(250);
    }
}


// --- Simulation ---

const int mapWidth = 7;
const int mapHeight = 7;

// Create instances here
var session = new GameSession(mapWidth, mapHeight);
var manager = new GameManager(session);
var printer = new MapPrinter();

// Initialize Map (Same as before)
for (int x = 0; x < mapWidth; x++) for (int y = 0; y < mapHeight; y++) session.Map[x, y] = TileType.Empty;
for (int x = 0; x < mapWidth; x++) { session.Map[x, 0] = TileType.Wall; session.Map[x, mapHeight - 1] = TileType.Wall; }
for (int y = 1; y < mapHeight - 1; y++) { session.Map[0, y] = TileType.Wall; session.Map[mapWidth - 1, y] = TileType.Wall; }
session.Map[3, 3] = TileType.Wall; session.Map[3, 4] = TileType.Wall; session.Map[1, 3] = TileType.DestructibleWall;
session.Map[2, 5] = TileType.Wall; session.Map[4, 2] = TileType.Wall; session.Map[3, 2] = TileType.DestructibleWall;
session.Map[4, 4] = TileType.DestructibleWall; session.Map[5, 3] = TileType.DestructibleWall;
session.Map[1, 2] = TileType.PowerUpBlastRadius; session.Map[1, 4] = TileType.PowerUpBombCount;

// Add Players
var player1 = new Player { Id = "1", X = 1, Y = 1 };
var player2 = new Player { Id = "2", X = mapWidth - 2, Y = mapHeight - 2 }; // P2 starts (5,5)
session.AddPlayer(player1);
session.AddPlayer(player2);

// Initial Display
Console.Clear();
Console.WriteLine($"Initial State ({mapWidth}x{mapHeight}):");
printer.PrintMap(session);
Console.WriteLine($">> P1 Initial Stats: R={player1.BlastRadius}, MaxB={player1.MaxBombs}, ActiveB={player1.ActiveBombsCount}");
Console.WriteLine($">> P2 Initial Stats: R={player2.BlastRadius}, MaxB={player2.MaxBombs}, ActiveB={player2.ActiveBombsCount}");
Console.WriteLine("Press Enter to start simulation...");
Console.ReadLine();


// --- Simulation Sequence (Passing manager, session, printer, player to helpers) ---
DoMove(manager, session, printer, player1, 0, 1);  // 1: P1 D to (1,2) - Collect 'F'. Tick 1.
DoMove(manager, session, printer, player2, -1, 0); // 2: P2 L to (4,5). Tick 2.
DoMove(manager, session, printer, player1, 0, 1);  // 3: P1 D to (1,3) [Hit X]. Tick 3.
DoMove(manager, session, printer, player2, 0, -1); // 4: P2 U to (4,4) [Hit X]. Tick 4.
DoMove(manager, session, printer, player1, 0, 1);  // 5: P1 D try (1,4) from (1,2) -> Collect '+'. Tick 5.
DoMove(manager, session, printer, player2, -1, 0); // 6: P2 L to (3,4) [Hit #]. Tick 6.
DoPlaceBomb(manager, session, printer, player1);   // 7: P1 Place Bomb 1 at (1,4). Tick 7.
DoMove(manager, session, printer, player1, 1, 0);  // 8: P1 R to (2,4). Tick 8.
DoPlaceBomb(manager, session, printer, player1);   // 9: P1 Place Bomb 2 at (2,4). Succeeds. Tick 9.
DoMove(manager, session, printer, player1, 1, 0);  // 10: P1 R to (3,4) [Hit #]. Tick 10.
DoMove(manager, session, printer, player1, 0, -1); // 11: P1 U to (2,3). Tick 11. (Bomb 1 explodes!)
DoMove(manager, session, printer, player2, 0, -1); // 12: P2 U to (4,4) [Hit X]. Tick 12.


Console.WriteLine("\nP1 placed two bombs & moved. Continuing ticks automatically...");
while (session.Bombs.Any() || session.Map.Cast<TileType>().Any(t => t == TileType.Explosion))
{
    // Pass null context to DoTick in loop as specific player action isn't relevant for stats display focus
    DoTick(manager, session, printer, null, false);
}
Console.WriteLine("Final cleanup tick...");
DoTick(manager, session, printer, null, true); // Final tick with pause


Console.WriteLine($"\n{actionCount} Actions performed, {tickCount} Ticks simulated. Press Enter to exit.");
Console.ReadLine();