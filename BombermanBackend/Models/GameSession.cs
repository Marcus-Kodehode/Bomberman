using System;
using System.Collections.Generic;
using System.Linq;

namespace BombermanBackend.Models
{
    public class GameSession
    {
        public TileType[,] Map { get; set; }
        public Dictionary<string, Player> Players { get; set; } = new();
        public List<Bomb> Bombs { get; set; } = new();

        public GameSession(int width, int height) { Map = new TileType[width, height]; }

        public void AddPlayer(Player player)
        {
            if (!Players.ContainsKey(player.Id))
            {
                Players[player.Id] = player;
                if (player.X >= 0 && player.X < Map.GetLength(0) && player.Y >= 0 && player.Y < Map.GetLength(1))
                {
                    if (Map[player.X, player.Y] == TileType.Empty) { Map[player.X, player.Y] = TileType.Player; }
                }
            }
        }

        public void AddBomb(string playerId, int x, int y, int blastRadius)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                Bomb bomb = new Bomb(playerId, x, y, blastRadius);
                Bombs.Add(bomb); Map[x, y] = TileType.Bomb;
            }
        }

        // Ensure both paths return a value
        public bool IsTileEmpty(int x, int y)
        {
            if (x < 0 || x >= Map.GetLength(0) || y < 0 || y >= Map.GetLength(1))
            {
                return false; // Return for out of bounds
            }
            // Return based on tile type if in bounds
            return Map[x, y] == TileType.Empty;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            // Removed DEBUG log for cleaner code now
            if (!Players.TryGetValue(playerId, out Player? player)) return false;

            int oldX = player.X; int oldY = player.Y;
            int dx = newX - oldX; int dy = newY - oldY;
            if (Math.Abs(dx) + Math.Abs(dy) != 1) return false; // Cardinal check

            if (newX < 0 || newX >= Map.GetLength(0) || newY < 0 || newY >= Map.GetLength(1)) return false; // Bounds check

            TileType targetTile = Map[newX, newY];
            bool isTargetTraversable = (targetTile == TileType.Empty ||
                                       targetTile == TileType.Bomb ||
                                       targetTile == TileType.PowerUpBombCount ||
                                       targetTile == TileType.PowerUpBlastRadius);

            if (!isTargetTraversable) return false; // Blocked

            // Apply Power-up Effect
            if (targetTile == TileType.PowerUpBombCount) { player.MaxBombs++; Console.WriteLine($"--- Player {player.Id} picked up PowerUpBombCount! MaxBombs: {player.MaxBombs} ---"); }
            else if (targetTile == TileType.PowerUpBlastRadius) { player.BlastRadius++; Console.WriteLine($"--- Player {player.Id} picked up PowerUpBlastRadius! BlastRadius: {player.BlastRadius} ---"); }

            player.X = newX; player.Y = newY; // Update position

            if (Map[oldX, oldY] == TileType.Player) { Map[oldX, oldY] = TileType.Empty; } // Clear old tile
            Map[newX, newY] = TileType.Player; // Update new tile

            return true;
        }
    }
}