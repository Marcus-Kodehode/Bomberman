using System;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public class MapPrinter
    {
        public void PrintMap(GameSession session)
        {
            Console.WriteLine();
            int width = session.Map.GetLength(0);
            int height = session.Map.GetLength(1);

            Console.Write("   ");
            for (int x = 0; x < width; x++) { Console.Write(x % 10); }
            Console.WriteLine();

            for (int y = 0; y < height; y++)
            {
                Console.Write($"{y % 10}: ");
                for (int x = 0; x < width; x++)
                {
                    var tile = session.Map[x, y];
                    Console.Write(tile switch
                    {
                        TileType.Wall => "#",
                        TileType.Bomb => "B",
                        TileType.Player => "P",
                        TileType.DestructibleWall => "X",
                        TileType.Explosion => "*",
                        TileType.PowerUpBombCount => "+", // Display '+' for Bomb Count
                        TileType.PowerUpBlastRadius => "F", // Display 'F' for Firepower/Blast Radius
                        _ => "."
                    });
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}