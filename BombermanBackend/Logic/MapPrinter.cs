using System;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public class MapPrinter
    {
        public void PrintMap(GameSession session)
        {
            Console.WriteLine();
            Console.Write("   ");
            for (int x = 0; x < session.Map.GetLength(0); x++)
            {
                Console.Write(x % 10);
            }
            Console.WriteLine();

            for (int y = 0; y < session.Map.GetLength(1); y++)
            {
                Console.Write($"{y % 10}: ");
                for (int x = 0; x < session.Map.GetLength(0); x++)
                {
                    var tile = session.Map[x, y];
                    Console.Write(tile switch
                    {
                        TileType.Wall => "#",
                        TileType.Bomb => "B",
                        TileType.Player => "P",
                        _ => "."
                    });
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
