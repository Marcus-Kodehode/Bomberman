using BombermanBackend.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BombermanBackend.Logic
{
    public class GameManager
    {
        private readonly GameSession _session;

        public GameManager(GameSession session) { _session = session; }

        public bool MovePlayer(string playerId, int newX, int newY) { return _session.MovePlayer(playerId, newX, newY); }

        public bool PlaceBomb(string playerId, int x, int y)
        {
            if (!_session.Players.TryGetValue(playerId, out Player? player)) return false;
            if (player.X != x || player.Y != y) return false;
            if (player.ActiveBombsCount >= player.MaxBombs) return false;
            _session.AddBomb(playerId, x, y, player.BlastRadius);
            player.ActiveBombsCount++;
            return true;
        }

        public void Tick()
        {
            CleanupExplosions();
            UpdateBombs();
        }

        private void CleanupExplosions()
        {
            int width = _session.Map.GetLength(0); int height = _session.Map.GetLength(1);
            for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) if (_session.Map[x, y] == TileType.Explosion) _session.Map[x, y] = TileType.Empty;
        }

        private void UpdateBombs()
        {
            List<Bomb> bombsReadyByFuse = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList()) // Use ToList() to safely check while main list exists
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsReadyByFuse.Add(bomb);
                    // DO NOT remove from _session.Bombs here yet
                }
            }

            if (!bombsReadyByFuse.Any()) return;

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsReadyByFuse);
            HashSet<Bomb> allDetonatedBombs = new HashSet<Bomb>(bombsReadyByFuse); // Track all bombs (initial + chained) that go off this tick

            while (detonationQueue.Count > 0)
            {
                var bombToDetonate = detonationQueue.Dequeue();
                // Pass detonationQueue and allDetonatedBombs set for chain reaction handling
                DetonateBomb(bombToDetonate, detonationQueue, allDetonatedBombs);
            }

            // --- Apply state changes AFTER processing all detonations ---

            // Decrement counts for owners
            var ownerCounts = allDetonatedBombs.GroupBy(b => b.OwnerId)
                                                .Select(g => new { OwnerId = g.Key, Count = g.Count() });
            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                }
            }

            // --- FIX: Explicitly remove detonated bombs using foreach ---
            // Remove all bombs that were processed (initial + chained)
            foreach (var bombToRemove in allDetonatedBombs)
            {
                _session.Bombs.Remove(bombToRemove); // Use standard Remove
            }
            // Removed: _session.Bombs.RemoveAll(b => allDetonatedBombs.Contains(b));
            // --- END FIX ---
        }

        // DetonateBomb calculates effects and queues chains, does not modify session lists directly
        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> allDetonatedBombs)
        {
            // Removed owner count decrement - handled after loop in UpdateBombs

            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));
            int[] dx = { 0, 0, 1, -1 }; int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int targetX = bomb.X + dx[i] * j; int targetY = bomb.Y + dy[i] * j;
                    if (targetX < 0 || targetX >= _session.Map.GetLength(0) || targetY < 0 || targetY >= _session.Map.GetLength(1)) break;
                    affectedTiles.Add((targetX, targetY));
                    TileType tileType = _session.Map[targetX, targetY];
                    if (tileType == TileType.Wall) break;
                    if (tileType == TileType.DestructibleWall) break;
                    if (tileType == TileType.Player || tileType == TileType.Bomb) break;
                }
            }

            List<Player> playersToRemove = new List<Player>();
            foreach (var (x, y) in affectedTiles)
            {
                TileType currentTileType = _session.Map[x, y];
                Player? playerOnTile = _session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y);
                if (playerOnTile != null && !playersToRemove.Contains(playerOnTile))
                {
                    Console.WriteLine($"--- Player {playerOnTile.Id} hit by explosion at ({x},{y})! ---");
                    playersToRemove.Add(playerOnTile);
                }

                // Check for *other* bombs still in the session list that might be hit
                Bomb? bombOnTile = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y && !allDetonatedBombs.Contains(b));
                if (bombOnTile != null)
                {
                    // Add to set of all detonated bombs if not already there.
                    // Add returns true if it was newly added.
                    if (allDetonatedBombs.Add(bombOnTile))
                    {
                        Console.WriteLine($"--- Chain reaction: Bomb at ({x},{y}) triggered by ({bomb.X},{bomb.Y})! ---");
                        detonationQueue.Enqueue(bombOnTile); // Add to queue for processing this tick
                    }
                }

                if (currentTileType != TileType.Wall)
                {
                    _session.Map[x, y] = TileType.Explosion;
                }
            }
            foreach (var playerToRemove in playersToRemove)
            {
                Console.WriteLine($"--- Player {playerToRemove.Id} removed due to explosion at ({bomb.X},{bomb.Y}) ---");
                _session.Players.Remove(playerToRemove.Id);
            }
        }
    }
}