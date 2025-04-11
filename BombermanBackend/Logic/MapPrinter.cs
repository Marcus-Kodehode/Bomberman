using System;
using System.Linq;
using BombermanBackend.Models;

namespace BombermanBackend.Logic
{
    public static class MapPrinter
    {
        public static void PrintMap(GameSession session, bool showCoords = false)
        {
            var map = session.Map;
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            Console.WriteLine();

            if (showCoords)
            {
                Console.Write("   ");
                for (int x = 0; x < width; x++)
                    Console.Write(x % 10);
                Console.WriteLine();
            }

            for (int y = 0; y < height; y++)
            {
                if (showCoords)
                    Console.Write($"{y % 10}: ");

                for (int x = 0; x < width; x++)
                {
                    var playerHere = session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y && p.IsAlive);
                    var bombHere = session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y);

                    if (playerHere != null)
                    {
                        Console.Write("P");
                    }
                    else if (bombHere != null)
                    {
                        Console.Write("B");
                    }
                    else
                    {
                        switch (map[x, y])
                        {
                            case TileType.Empty: Console.Write("."); break;
                            case TileType.Wall: Console.Write("#"); break;
                            case TileType.Destructible: Console.Write("D"); break;
                            case TileType.Explosion: Console.Write("*"); break;
                            default: Console.Write("?"); break;
                        }
                    }
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }
    }
}
