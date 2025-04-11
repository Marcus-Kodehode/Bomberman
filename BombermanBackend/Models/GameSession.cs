using System;
using System.Collections.Generic;
using System.Linq;

namespace BombermanBackend.Models
{
    public class GameSession
    {
        public string GameId { get; set; } = Guid.NewGuid().ToString();
        public TileType[,] Map { get; set; }
        public Dictionary<string, Player> Players { get; set; } = new();
        public List<Bomb> Bombs { get; set; } = new();

        public GameSession(int width, int height)
        {
            Map = new TileType[width, height];

            // Fill with empty tiles
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    Map[x, y] = TileType.Empty;

            // Add static walls every 2 tiles
            for (int y = 0; y < height; y += 2)
                for (int x = 0; x < width; x += 2)
                    Map[x, y] = TileType.Wall;
        }

        public void AddPlayer(string playerId, int startX, int startY)
        {
            Players[playerId] = new Player { Id = playerId, X = startX, Y = startY };
        }

        public bool AddBomb(string playerId, int x, int y)
        {
            if (Bombs.Any(b => b.X == x && b.Y == y))
                return false;

            Bombs.Add(new Bomb { OwnerId = playerId, X = x, Y = y });
            Map[x, y] = TileType.Bomb;
            return true;
        }
    }
}
