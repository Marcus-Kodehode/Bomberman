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
                    // Ensure player doesn't spawn inside a wall
                    if (Map[player.X, player.Y] == TileType.Empty)
                    {
                        Map[player.X, player.Y] = TileType.Player;
                    }
                }
            }
        }

        public void AddBomb(string playerId, int x, int y)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                // Consider checking if Map[x,y] allows bombs (e.g., not Wall)
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

            TileType targetTile = Map[newX, newY];

            // --- MODIFICATION START ---
            // Player can only move onto Empty or existing Bomb tiles
            bool isTargetTraversable = (targetTile == TileType.Empty || targetTile == TileType.Bomb);
            // --- MODIFICATION END ---

            if (!isTargetTraversable)
            {
                // Blocked by Wall, DestructibleWall, Player, etc.
                return false;
            }

            var player = Players[playerId];
            int oldX = player.X;
            int oldY = player.Y;

            // Only clear the old tile if the player was the only thing there
            if (Map[oldX, oldY] == TileType.Player)
            {
                Map[oldX, oldY] = TileType.Empty;
            }

            player.X = newX;
            player.Y = newY;

            // Place player on new tile
            Map[newX, newY] = TileType.Player;

            return true;
        }
    }
}