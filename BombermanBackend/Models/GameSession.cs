using System.Collections.Generic;
using System.Linq;

namespace BombermanBackend.Models
{
    public class GameSession
    {
        public TileType[,] Map { get; set; }
        public Dictionary<string, Player> Players { get; set; } = new();
        public List<Bomb> Bombs { get; set; } = new();

        public GameSession(int width, int height)
        {
            Map = new TileType[width, height];
        }

        public void AddPlayer(Player player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Players[player.Id] = player;
                if (player.X >= 0 && player.X < Map.GetLength(0) && player.Y >= 0 && player.Y < Map.GetLength(1))
                {
                    Map[player.X, player.Y] = TileType.Player;
                }
            }
        }

        // AddBomb remains simple - GameManager now handles validation before calling this
        public void AddBomb(string playerId, int x, int y)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                Bomb bomb = new Bomb(playerId, x, y);
                Bombs.Add(bomb);
                Map[x, y] = TileType.Bomb;
            }
        }

        public bool IsTileEmpty(int x, int y)
        {
            if (x < 0 || x >= Map.GetLength(0) || y < 0 || y >= Map.GetLength(1))
                return false;
            return Map[x, y] == TileType.Empty;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            if (!Players.ContainsKey(playerId) ||
                newX < 0 || newX >= Map.GetLength(0) ||
                newY < 0 || newY >= Map.GetLength(1))
            {
                return false;
            }

            bool isTargetTraversable = (Map[newX, newY] == TileType.Empty || Map[newX, newY] == TileType.Bomb);

            if (!isTargetTraversable)
            {
                return false;
            }

            var player = Players[playerId];
            int oldX = player.X;
            int oldY = player.Y;

            if (Map[oldX, oldY] == TileType.Player)
            {
                Map[oldX, oldY] = TileType.Empty;
            }

            player.X = newX;
            player.Y = newY;

            Map[newX, newY] = TileType.Player;

            return true;
        }
    }
}