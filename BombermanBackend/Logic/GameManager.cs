using BombermanBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BombermanBackend.Logic
{
    public class GameManager
    {
        private readonly GameSession _session;

        public GameManager(GameSession session)
        {
            _session = session;
        }

        public bool MovePlayer(string playerId, int newX, int newY)
        {
            return _session.MovePlayer(playerId, newX, newY);
        }

        public bool PlaceBomb(string playerId, int x, int y)
        {
            if (!_session.Players.TryGetValue(playerId, out Player? player)) return false;
            if (player.X != x || player.Y != y) return false; // Must place at own feet
            if (player.ActiveBombsCount >= player.MaxBombs) return false; // Bomb limit

            _session.AddBomb(playerId, x, y);
            player.ActiveBombsCount++;
            return true;
        }

        // Tick orchestrates game updates
        public void Tick()
        {
            CleanupExplosions(); // Clear '*' from previous tick first
            UpdateBombs();       // Update fuse timers and handle detonations
        }

        // Clears temporary explosion tiles
        private void CleanupExplosions()
        {
            int width = _session.Map.GetLength(0);
            int height = _session.Map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_session.Map[x, y] == TileType.Explosion)
                    {
                        _session.Map[x, y] = TileType.Empty;
                    }
                }
            }
        }

        // Updates bomb timers and triggers detonations
        private void UpdateBombs()
        {
            List<Bomb> bombsReadyToDetonate = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList()) // Iterate copy
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsReadyToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb); // Remove from active list
                }
            }

            // Use queue and set for handling chain reactions within the same tick
            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsReadyToDetonate);
            HashSet<Bomb> processedBombs = new HashSet<Bomb>(bombsReadyToDetonate); // Track already processed

            while (detonationQueue.Count > 0)
            {
                var bombToDetonate = detonationQueue.Dequeue();
                // Pass the queue and set to handle potential chain reactions
                DetonateBomb(bombToDetonate, detonationQueue, processedBombs);
            }
        }

        // Handles the actual explosion logic
        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> processedBombs)
        {
            // Ensure owner's count is decremented
            if (_session.Players.TryGetValue(bomb.OwnerId, out Player? owner))
            {
                if (owner.ActiveBombsCount > 0) owner.ActiveBombsCount--;
            }

            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y)); // Bomb's own tile is always affected

            int[] dx = { 0, 0, 1, -1 }; // Directions array
            int[] dy = { 1, -1, 0, 0 }; // Down, Up, Right, Left

            // Calculate blast path & affected tiles for each direction
            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int targetX = bomb.X + dx[i] * j;
                    int targetY = bomb.Y + dy[i] * j;

                    // Check map boundaries
                    if (targetX < 0 || targetX >= _session.Map.GetLength(0) || targetY < 0 || targetY >= _session.Map.GetLength(1))
                        break; // Stop this direction if out of bounds

                    // Add the tile to the set of affected tiles for later processing
                    affectedTiles.Add((targetX, targetY));

                    TileType tileType = _session.Map[targetX, targetY];

                    // Stop blast propagation if it hits an indestructible wall
                    if (tileType == TileType.Wall)
                        break;

                    // Stop blast propagation *after* hitting a destructible wall
                    if (tileType == TileType.DestructibleWall)
                        break;

                    // Stop blast propagation *after* hitting a player or another bomb
                    if (tileType == TileType.Player || tileType == TileType.Bomb)
                        break;
                }
            }

            // Process effects on all affected tiles
            List<Player> playersToRemove = new List<Player>();
            foreach (var (x, y) in affectedTiles)
            {
                TileType currentTileType = _session.Map[x, y]; // Get type before modifying

                // Check for players on this tile
                Player? playerOnTile = _session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y);
                if (playerOnTile != null && !playersToRemove.Contains(playerOnTile))
                {
                    Console.WriteLine($"--- Player {playerOnTile.Id} hit by explosion at ({x},{y})! ---");
                    playersToRemove.Add(playerOnTile);
                }

                // Check for other bombs for chain reaction
                Bomb? bombOnTile = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                if (bombOnTile != null)
                {
                    // Add to detonation queue if not already processed/queued this tick
                    if (processedBombs.Add(bombOnTile)) // Add returns true if newly added
                    {
                        Console.WriteLine($"--- Chain reaction: Bomb at ({x},{y}) triggered! ---");
                        _session.Bombs.Remove(bombOnTile); // Remove from active list now
                        detonationQueue.Enqueue(bombOnTile); // Add to queue for detonation this tick
                    }
                }

                // Set tile to Explosion, unless it's an indestructible Wall
                if (currentTileType != TileType.Wall)
                {
                    _session.Map[x, y] = TileType.Explosion;
                    // TODO: If currentTileType == TileType.DestructibleWall, maybe drop powerup?
                }
            }

            // Remove hit players from the game session
            foreach (var playerToRemove in playersToRemove)
            {
                _session.Players.Remove(playerToRemove.Id);
                Console.WriteLine($"--- Player {playerToRemove.Id} removed from game. ---");
                // TODO: Implement player death state / respawn logic more formally
            }
        }
    }
}