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
                Console.Write(x % 10); // Print last digit of column number
            }
            Console.WriteLine();

            // Print map rows
            for (int y = 0; y < height; y++)
            {
                Console.Write($"{y % 10}: "); // Print last digit of row number
                for (int x = 0; x < width; x++)
                {
                    var tile = session.Map[x, y];
                    Console.Write(tile switch
                    {
                        TileType.Wall => "#",             // Indestructible
                        TileType.Bomb => "B",
                        TileType.Player => "P",
                        TileType.DestructibleWall => "X", // Destructible
                        _ => "."                          // Empty and others default to '.'
                    });
                }
                Console.WriteLine(); // Newline after each row
            }
            Console.WriteLine(); // Add a blank line for spacing
        }
    }
}