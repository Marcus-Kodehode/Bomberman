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

        public void AddBomb(string playerId, int x, int y)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                Bomb bomb = new Bomb(playerId, x, y); // Uses default fuse (5)
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

            // --- MODIFICATION START ---
            // Allow movement onto Empty or Bomb tiles
            bool isTargetTraversable = (Map[newX, newY] == TileType.Empty || Map[newX, newY] == TileType.Bomb);
            // --- MODIFICATION END ---

            if (!isTargetTraversable)
            {
                return false; // Blocked by Wall, Player, etc.
            }

            var player = Players[playerId];
            int oldX = player.X;
            int oldY = player.Y;

            // Only clear the old tile if the player was the only thing there
            if (Map[oldX, oldY] == TileType.Player)
            {
                Map[oldX, oldY] = TileType.Empty;
            }
            // If Map[oldX, oldY] was TileType.Bomb, leave it.

            player.X = newX;
            player.Y = newY;

            // Place player on new tile (overwriting if it was a bomb visually)
            Map[newX, newY] = TileType.Player;

            return true;
        }
    }
}