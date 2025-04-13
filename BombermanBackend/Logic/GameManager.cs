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
            // --- FIX: Allow placing bomb only if player is ON the target tile ---
            if (player.X != x || player.Y != y) return false;
            // --- END FIX ---
            if (player.ActiveBombsCount >= player.MaxBombs) return false;

            // --- FIX: Pass player's CURRENT blast radius to the bomb constructor ---
            _session.AddBomb(playerId, x, y, player.BlastRadius);
            // --- END FIX ---
            player.ActiveBombsCount++;
            return true;
        }

        public void Tick()
        {
            CleanupExplosions(); // Clean up explosions from previous tick first
            UpdateBombs();       // Then update and detonate bombs
        }

        // Set explosion tiles back to empty
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


        private void UpdateBombs()
        {
            // 1. Decrement fuse timers for all bombs
            foreach (var bomb in _session.Bombs)
            {
                bomb.RemainingFuseTicks--;
            }

            // 2. Collect bombs that are ready to detonate (fuse <= 0)
            List<Bomb> bombsToDetonate = _session.Bombs.Where(b => b.RemainingFuseTicks <= 0).ToList();

            if (!bombsToDetonate.Any())
            {
                return; // No bombs to detonate this tick
            }

            // 3. Handle Detonations & Chain Reactions
            // Use a queue for BFS-like chain reaction handling
            // Use a set to track bombs already detonated in this tick to prevent infinite loops
            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsToDetonate);
            HashSet<Bomb> detonatedBombs = new HashSet<Bomb>(); // Track bombs already processed
            List<string> ownerIdsOfDetonatedBombs = new List<string>(); // To decrement player counts later


            while (detonationQueue.Count > 0)
            {
                var bomb = detonationQueue.Dequeue();

                // Ensure we only process each bomb detonation once per tick
                if (detonatedBombs.Add(bomb))
                {
                    ownerIdsOfDetonatedBombs.Add(bomb.OwnerId); // Track owner for count adjustment
                    DetonateBomb(bomb, detonationQueue, detonatedBombs); // Handles explosion tiles & chain reactions
                }
            }

            // 4. Adjust player active bomb counts
            // Group by owner ID to correctly decrement counts even if multiple bombs explode
            var ownerCounts = ownerIdsOfDetonatedBombs.GroupBy(id => id)
                                                    .Select(g => new { OwnerId = g.Key, Count = g.Count() });

            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                }
            }

            // 5. Remove the detonated bombs from the main session list
            _session.Bombs.RemoveAll(b => detonatedBombs.Contains(b));

            // Note: We don't call CleanupExplosions() here. Explosions persist for one tick
            // and are cleaned at the *start* of the next tick.
        }


        // Handles the explosion effect of a single bomb
        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> detonatedBombs)
        {
            // Use a set to efficiently track all tiles affected by *this specific* bomb's explosion
            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();

            // The bomb's own location is always affected
            affectedTiles.Add((bomb.X, bomb.Y));

            // Directions (Right, Left, Down, Up)
            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            // Calculate explosion path in each of the 4 directions
            for (int i = 0; i < 4; i++) // Loop through directions
            {
                for (int j = 1; j <= bomb.BlastRadius; j++) // Loop through radius steps
                {
                    int currentX = bomb.X + dx[i] * j;
                    int currentY = bomb.Y + dy[i] * j;

                    // Check bounds
                    if (currentX < 0 || currentX >= _session.Map.GetLength(0) || currentY < 0 || currentY >= _session.Map.GetLength(1))
                    {
                        break; // Stop propagation if out of bounds
                    }

                    affectedTiles.Add((currentX, currentY)); // Mark tile as affected

                    TileType tile = _session.Map[currentX, currentY];

                    // Stop propagation if it hits an indestructible wall
                    if (tile == TileType.Wall)
                    {
                        break;
                    }

                    // If it hits a destructible wall, stop propagation (wall absorbs blast)
                    if (tile == TileType.DestructibleWall)
                    {
                        // Note: Wall turns into explosion, handled later
                        break;
                    }

                    // If it hits another bomb, trigger a chain reaction
                    if (tile == TileType.Bomb)
                    {
                        // Find the bomb object at this location
                        var bombToChain = _session.Bombs.FirstOrDefault(b => b.X == currentX && b.Y == currentY);

                        // Add to queue ONLY if it exists and hasn't already been processed this tick
                        if (bombToChain != null && !detonatedBombs.Contains(bombToChain) && !detonationQueue.Contains(bombToChain))
                        {
                            detonationQueue.Enqueue(bombToChain);
                        }
                        // Note: Bomb tile turns into explosion, handled later
                        break; // Explosion stops propagation here too
                    }

                    // --- FIX: Prevent explosion propagation through existing explosion tiles ---
                    // --- This prevents re-adding players who might have been removed by a prior chain reaction explosion ---
                    if (tile == TileType.Explosion)
                    {
                        break; // Stop if we hit an existing explosion tile from this tick's chain
                    }
                    // --- END FIX ---
                }
            }

            // --- Centralized Handling of Affected Tiles ---

            // 1. Handle Player Hits FIRST (before changing tiles)
            List<string> playersHitIds = new List<string>();
            foreach (var (x, y) in affectedTiles)
            {
                // Find players currently at this tile
                var playersAtTile = _session.Players.Values.Where(p => p.X == x && p.Y == y).ToList();
                foreach (var player in playersAtTile)
                {
                    if (!playersHitIds.Contains(player.Id)) // Ensure player is only added once
                    {
                        playersHitIds.Add(player.Id);
                    }
                }
            }
            // Remove hit players from the game session
            foreach (string playerId in playersHitIds)
            {
                _session.Players.Remove(playerId);
                // Optional: Log player removal
                // Console.WriteLine($"--- Player {playerId} removed by explosion ---");
            }


            // 2. Set all affected tiles to Explosion state
            // This overwrites Empty, DestructibleWall, collected PowerUps, and potentially chained Bombs
            foreach (var (x, y) in affectedTiles)
            {
                // Check bounds again just in case, though should be correct from propagation logic
                if (x >= 0 && x < _session.Map.GetLength(0) && y >= 0 && y < _session.Map.GetLength(1))
                {
                    // Don't overwrite indestructible walls
                    if (_session.Map[x, y] != TileType.Wall)
                    {
                        _session.Map[x, y] = TileType.Explosion;
                    }
                }
            }
        }
    }
}