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
                    // Only place player on map if the starting tile is empty
                    if (Map[player.X, player.Y] == TileType.Empty)
                    {
                        Map[player.X, player.Y] = TileType.Player;
                    }
                    // Consider logging a warning if the start tile is not empty
                }
            }
        }

        public void AddBomb(string playerId, int x, int y, int blastRadius)
        {
            if (x >= 0 && x < Map.GetLength(0) && y >= 0 && y < Map.GetLength(1))
            {
                // Consider checking if Map[x,y] is already a bomb or wall?
                Bomb bomb = new Bomb(playerId, x, y, blastRadius);
                Bombs.Add(bomb);
                Map[x, y] = TileType.Bomb; // Overwrite tile with bomb
            }
        }

        // Helper to check if a tile is within bounds and is empty
        public bool IsTileEmpty(int x, int y)
        {
            if (x < 0 || x >= Map.GetLength(0) || y < 0 || y >= Map.GetLength(1))
            {
                return false; // Out of bounds is not empty
            }
            return Map[x, y] == TileType.Empty;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            if (!Players.TryGetValue(playerId, out Player? player)) return false;

            int oldX = player.X;
            int oldY = player.Y;

            // Basic validation: Ensure move is cardinal and only one step
            int dx = newX - oldX;
            int dy = newY - oldY;
            if (Math.Abs(dx) + Math.Abs(dy) != 1) return false;

            // Bounds check for the new position
            if (newX < 0 || newX >= Map.GetLength(0) || newY < 0 || newY >= Map.GetLength(1)) return false;

            TileType targetTile = Map[newX, newY];

            // Check if the target tile is traversable
            // --- FIX: Allow moving onto explosion (player might die) ---
            bool isTargetTraversable = (targetTile == TileType.Empty ||
                                       targetTile == TileType.Bomb || // Players can walk onto bombs
                                       targetTile == TileType.PowerUpBombCount ||
                                       targetTile == TileType.PowerUpBlastRadius ||
                                       targetTile == TileType.Explosion); // Allow moving onto explosion
            // --- END FIX ---

            if (!isTargetTraversable) return false; // Blocked by Wall, DestructibleWall, or another Player

            // Apply Power-up Effect (if applicable) and remove it from map conceptually
            if (targetTile == TileType.PowerUpBombCount)
            {
                player.MaxBombs++;
                Console.WriteLine($"--- Player {player.Id} picked up PowerUpBombCount! MaxBombs: {player.MaxBombs} ---"); // Note: Includes debug messages
            }
            else if (targetTile == TileType.PowerUpBlastRadius)
            {
                player.BlastRadius++;
                Console.WriteLine($"--- Player {player.Id} picked up PowerUpBlastRadius! BlastRadius: {player.BlastRadius} ---"); // Note: Includes debug messages
            }

            // Update player's position internally
            player.X = newX;
            player.Y = newY;

            // Update the map representation
            // Clear the old tile *only if* it was the player tile (don't clear bombs player moved off)
            if (Map[oldX, oldY] == TileType.Player)
            {
                Map[oldX, oldY] = TileType.Empty;
            }
            // Place player on the new tile (this overwrites Empty, Bomb, Powerup, Explosion)
            Map[newX, newY] = TileType.Player;

            return true;
        }
    }
}