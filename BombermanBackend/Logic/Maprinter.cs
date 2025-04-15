using System;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public class MapPrinter
    {
        public void PrintMap(GameSession session)
        {
            Console.WriteLine(); // Add a blank line for spacing
            int width = session.Map.GetLength(0);
            int height = session.Map.GetLength(1);

            // Print header row
            Console.Write("   "); // Indent for row numbers
            for (int x = 0; x < width; x++)
            {
                Console.Write(x % 10); // Display single digit column index
            }
            Console.WriteLine();

            // Print map rows with row numbers
            for (int y = 0; y < height; y++)
            {
                Console.Write($"{y % 10}: "); // Display single digit row index
                for (int x = 0; x < width; x++)
                {
                    var tile = session.Map[x, y];
                    // Use a switch expression for cleaner character mapping
                    Console.Write(tile switch
                    {
                        TileType.Wall => "#",
                        TileType.Bomb => "B",
                        TileType.Player => "P",
                        TileType.DestructibleWall => "X",
                        TileType.Explosion => "*",
                        TileType.PowerUpBombCount => "+", // Display '+' for Bomb Count
                        TileType.PowerUpBlastRadius => "F", // Display 'F' for Firepower/Blast Radius
                        _ => "." // Default case for Empty and any future TileTypes
                    });
                }
                Console.WriteLine(); // New line after each row
            }
            Console.WriteLine(); // Add a blank line for spacing after the map
        }
    }
}