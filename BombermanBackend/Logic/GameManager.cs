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
            CleanupExplosions(); // Clean up explosions from previous tick first
            UpdateBombs(); // Then update and detonate bombs
        }

        private void CleanupExplosions()
        {
            Console.WriteLine("DEBUG: --- CleanupExplosions() ---");
            int width = _session.Map.GetLength(0);
            int height = _session.Map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_session.Map[x, y] == TileType.Explosion)
                    {
                        Console.WriteLine($" DEBUG: Cleaning up explosion at ({x},{y})");
                        _session.Map[x, y] = TileType.Empty;
                    }
                }
            }
            Console.WriteLine("DEBUG: --- CleanupExplosions() Complete ---");
        }

        private void UpdateBombs()
        {
            Console.WriteLine("DEBUG: --- UpdateBombs() ---");
            // Decrement fuse timers
            foreach (var bomb in _session.Bombs)
            {
                bomb.RemainingFuseTicks--;
                Console.WriteLine($" DEBUG: Bomb at ({bomb.X}, {bomb.Y}) fuse: {bomb.RemainingFuseTicks}");
            }

            // Collect bombs that are ready to detonate
            List<Bomb> bombsToDetonate = _session.Bombs.Where(b => b.RemainingFuseTicks <= 0).ToList();

            if (!bombsToDetonate.Any())
            {
                Console.WriteLine(" DEBUG: No bombs to detonate.");
                Console.WriteLine("DEBUG: --- UpdateBombs() Complete ---");
                return; // No bombs to detonate this tick
            }

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsToDetonate);
            HashSet<Bomb> detonatedBombs = new HashSet<Bomb>();
            List<string> ownerIdsOfDetonatedBombs = new List<string>();

            while (detonationQueue.Count > 0)
            {
                var bomb = detonationQueue.Dequeue();
                if (detonatedBombs.Add(bomb)) // Process each bomb only once
                {
                    ownerIdsOfDetonatedBombs.Add(bomb.OwnerId);
                    DetonateBomb(bomb, detonationQueue, detonatedBombs);
                }
                Console.WriteLine($" DEBUG: DetonationQueue count: {detonationQueue.Count}, detonatedBombs count: {detonatedBombs.Count}");
            }

            // Adjust player active bomb counts
            var ownerCounts = ownerIdsOfDetonatedBombs.GroupBy(id => id).Select(g => new { OwnerId = g.Key, Count = g.Count() });
            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                    Console.WriteLine($" DEBUG: Player {owner.Id} ActiveBombsCount adjusted to {owner.ActiveBombsCount}");
                }
            }

            // Remove detonated bombs from the session
            _session.Bombs.RemoveAll(b => detonatedBombs.Contains(b));
            Console.WriteLine($" DEBUG: Bombs remaining after detonation: {_session.Bombs.Count}");

            // Note: No CleanupExplosions() call here - explosions will persist until next tick
            Console.WriteLine("DEBUG: --- UpdateBombs() Complete ---");
        }

        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> detonatedBombs)
        {
            Console.WriteLine($" DEBUG: Detonating bomb at ({bomb.X},{bomb.Y})");
            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));
            
            // Set the bomb's own tile to explosion immediately
            _session.Map[bomb.X, bomb.Y] = TileType.Explosion;

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int x = bomb.X + dx[i] * j;
                    int y = bomb.Y + dy[i] * j;

                    if (x < 0 || x >= _session.Map.GetLength(0) || y < 0 || y >= _session.Map.GetLength(1))
                    {
                        Console.WriteLine($" DEBUG: Explosion out of bounds at ({x},{y})");
                        break; // Stop going out of bounds
                    }

                    affectedTiles.Add((x, y));

                    TileType tile = _session.Map[x, y];
                    Console.WriteLine($"  DEBUG: Detonating bomb at ({bomb.X},{bomb.Y}). Checking ({x},{y}). Tile: {tile}");

                    if (tile == TileType.Wall)
                    {
                        Console.WriteLine($"  DEBUG: Explosion stopped by Wall at ({x},{y})");
                        break; // Wall blocks further explosion
                    }

                    if (tile == TileType.DestructibleWall)
                    {
                        _session.Map[x, y] = TileType.Explosion;
                        Console.WriteLine($"  DEBUG: Set ({x},{y}) to Explosion (DestructibleWall).");
                        break; // Destructible wall is destroyed, explosion stops
                    }

                    if (tile == TileType.Bomb)
                    {
                        // Trigger chain reaction (if not already detonating)
                        var bombToChain = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y && !detonatedBombs.Contains(b));
                        if (bombToChain != null)
                        {
                            detonationQueue.Enqueue(bombToChain);
                            detonatedBombs.Add(bombToChain);
                            Console.WriteLine($"  DEBUG: Bomb at ({x},{y}) added to detonationQueue due to chain reaction.");
                        }
                        _session.Map[x, y] = TileType.Explosion; // Convert bomb tile to explosion
                        break; // Bomb hit, explosion stops
                    }

                    _session.Map[x, y] = TileType.Explosion;
                    Console.WriteLine($"  DEBUG: Set ({x},{y}) to Explosion.");
                }
            }

            // STEP 1: First set all affected tiles to explosions
            Console.WriteLine($"  DEBUG: Setting all {affectedTiles.Count} affected tiles to explosions");
            foreach (var (x, y) in affectedTiles)
            {
                _session.Map[x, y] = TileType.Explosion;
                Console.WriteLine($"  DEBUG: Set ({x},{y}) to Explosion");
            }

            // STEP 2: Handle player hits
            Console.WriteLine($"  DEBUG: Players before hit check: {_session.Players.Count}");
            
            // Debug log all player positions for better diagnostics
            Console.WriteLine("  DEBUG: Current player positions:");
            foreach (var player in _session.Players.Values)
            {
                Console.WriteLine($"  DEBUG: Player {player.Id} is at position ({player.X},{player.Y})");
            }
            
            // Use a separate list to track hit players to avoid collection modification issues
            List<Player> playersToRemove = new List<Player>();
            
            // Check each affected tile for players
            foreach (var (x, y) in affectedTiles)
            {
                // Find any players at this position
                foreach (var player in _session.Players.Values)
                {
                    if (player.X == x && player.Y == y && !playersToRemove.Contains(player))
                    {
                        Console.WriteLine($"  DEBUG: Player {player.Id} found at explosion coordinates ({x},{y}), adding to removal list");
                        playersToRemove.Add(player);
                    }
                }
            }
            
            // Now remove all hit players in a separate step to avoid collection modification issues
            Console.WriteLine($"  DEBUG: Found {playersToRemove.Count} players to remove");
            foreach (var player in playersToRemove)
            {
                bool removed = _session.Players.Remove(player.Id);
                Console.WriteLine($"  DEBUG: Player {player.Id} hit by explosion at ({player.X},{player.Y})! Removed: {removed}");
            }
            
            Console.WriteLine($"  DEBUG: Players after hit check: {_session.Players.Count}, Players hit: {playersToRemove.Count}");
            Console.WriteLine($"  DEBUG: Detonated bombs count: {detonatedBombs.Count}");
        }
    }
}
