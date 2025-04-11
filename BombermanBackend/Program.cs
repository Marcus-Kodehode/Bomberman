using System;
using BombermanBackend.Models;
using BombermanBackend.Logic;

var session = new GameSession(11, 11);
session.AddPlayer("player1", 1, 1);
var manager = new GameManager(session);
var player = session.Players["player1"];

// Step 1: Move right 5 times
for (int i = 1; i <= 5; i++)
{
    Console.WriteLine($"Move {i}: Trying to move right...");

    bool moved = manager.MovePlayer("player1", "right");

    if (moved)
        Console.WriteLine($"✅ Success! New position: ({player.X}, {player.Y})");
    else
    {
        Console.WriteLine($"❌ Blocked! Stayed at: ({player.X}, {player.Y})");
        break;
    }

    MapPrinter.PrintMap(session, showCoords: true);
}

// Step 2: Place bomb at current position (6,1)
Console.WriteLine("Placing bomb at current position...");
manager.PlaceBomb("player1");
MapPrinter.PrintMap(session, true);

// Step 3: Move back to (5,1)
Console.WriteLine("Step back:");
manager.MovePlayer("player1", "left");
MapPrinter.PrintMap(session, true);

// Step 4: Move up to (5,0)
Console.WriteLine("Move up:");
manager.MovePlayer("player1", "up");
MapPrinter.PrintMap(session, true);

// Step 5: Try to move right into wall (6,0)
Console.WriteLine("Try to move right into a wall:");
bool wallTest = manager.MovePlayer("player1", "right");

if (wallTest)
    Console.WriteLine("⚠️ Unexpected! Player moved into wall!");
else
    Console.WriteLine("✅ Correctly blocked by wall.");

MapPrinter.PrintMap(session, true);
Console.WriteLine("Game session ended. Press any key to exit.");
Console.ReadKey();
// End of the program
// This is a simple console application that simulates a Bomberman game session.