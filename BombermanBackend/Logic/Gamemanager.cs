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
            Console.WriteLine("DEBUG: --- CleanupExplosions() ---"); // Note: Includes debug messages
            int width = _session.Map.GetLength(0);
            int height = _session.Map.GetLength(1);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_session.Map[x, y] == TileType.Explosion)
                    {
                        Console.WriteLine($" DEBUG: Cleaning up explosion at ({x},{y})"); // Note: Includes debug messages
                        _session.Map[x, y] = TileType.Empty;
                    }
                }
            }
            Console.WriteLine("DEBUG: --- CleanupExplosions() Complete ---"); // Note: Includes debug messages
        }


        private void UpdateBombs()
        {
            Console.WriteLine("DEBUG: --- UpdateBombs() ---"); // Note: Includes debug messages
            // 1. Decrement fuse timers for all bombs
            foreach (var bomb in _session.Bombs)
            {
                bomb.RemainingFuseTicks--;
                Console.WriteLine($" DEBUG: Bomb at ({bomb.X}, {bomb.Y}) fuse: {bomb.RemainingFuseTicks}"); // Note: Includes debug messages
            }

            // 2. Collect bombs that are ready to detonate (fuse <= 0)
            List<Bomb> bombsToDetonate = _session.Bombs.Where(b => b.RemainingFuseTicks <= 0).ToList();

            if (!bombsToDetonate.Any())
            {
                Console.WriteLine(" DEBUG: No bombs to detonate."); // Note: Includes debug messages
                Console.WriteLine("DEBUG: --- UpdateBombs() Complete ---"); // Note: Includes debug messages
                return; // No bombs to detonate this tick
            }

            // 3. Handle Detonations & Chain Reactions
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
                Console.WriteLine($" DEBUG: DetonationQueue count: {detonationQueue.Count}, detonatedBombs count: {detonatedBombs.Count}"); // Note: Includes debug messages
            }

            // 4. Adjust player active bomb counts
            var ownerCounts = ownerIdsOfDetonatedBombs.GroupBy(id => id)
                                                    .Select(g => new { OwnerId = g.Key, Count = g.Count() });

            foreach (var ownerInfo in ownerCounts)
            {
                if (_session.Players.TryGetValue(ownerInfo.OwnerId, out Player? owner))
                {
                    owner.ActiveBombsCount = Math.Max(0, owner.ActiveBombsCount - ownerInfo.Count);
                    Console.WriteLine($" DEBUG: Player {owner.Id} ActiveBombsCount adjusted to {owner.ActiveBombsCount}"); // Note: Includes debug messages
                }
            }

            // 5. Remove the detonated bombs from the main session list
            _session.Bombs.RemoveAll(b => detonatedBombs.Contains(b));
            Console.WriteLine($" DEBUG: Bombs remaining after detonation: {_session.Bombs.Count}"); // Note: Includes debug messages

            Console.WriteLine("DEBUG: --- UpdateBombs() Complete ---"); // Note: Includes debug messages
        }


        // Handles the explosion effect of a single bomb
        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> detonatedBombs)
        {
            Console.WriteLine($" DEBUG: Detonating bomb at ({bomb.X},{bomb.Y})"); // Note: Includes debug messages
            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));

            // Set the bomb's own tile to explosion immediately
            // Check bounds just in case, though bomb position should always be valid
            if (bomb.X >= 0 && bomb.X < _session.Map.GetLength(0) && bomb.Y >= 0 && bomb.Y < _session.Map.GetLength(1))
            {
                _session.Map[bomb.X, bomb.Y] = TileType.Explosion;
            }


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
                        Console.WriteLine($" DEBUG: Explosion out of bounds at ({currentX},{currentY})"); // Note: Includes debug messages
                        break; // Stop propagation if out of bounds
                    }

                    affectedTiles.Add((currentX, currentY)); // Mark tile as affected for player hit check later

                    TileType tile = _session.Map[currentX, currentY];
                    Console.WriteLine($"  DEBUG: Checking ({currentX},{currentY}). Tile: {tile}"); // Note: Includes debug messages


                    // Stop propagation if it hits an indestructible wall
                    if (tile == TileType.Wall)
                    {
                        Console.WriteLine($"  DEBUG: Explosion stopped by Wall at ({currentX},{currentY})"); // Note: Includes debug messages
                        break;
                    }

                    // If it hits a destructible wall, destroy it and stop propagation
                    if (tile == TileType.DestructibleWall)
                    {
                        _session.Map[currentX, currentY] = TileType.Explosion; // Turn wall into explosion
                        Console.WriteLine($"  DEBUG: Set ({currentX},{currentY}) to Explosion (from DestructibleWall)."); // Note: Includes debug messages
                        break;
                    }

                    // If it hits another bomb, trigger chain reaction and stop propagation
                    if (tile == TileType.Bomb)
                    {
                        var bombToChain = _session.Bombs.FirstOrDefault(b => b.X == currentX && b.Y == currentY);
                        if (bombToChain != null && !detonatedBombs.Contains(bombToChain) && !detonationQueue.Contains(bombToChain))
                        {
                            // Only add to queue if not already processed or queued
                            detonationQueue.Enqueue(bombToChain);
                            Console.WriteLine($"  DEBUG: Chain reaction: Adding Bomb at ({currentX},{currentY}) to queue."); // Note: Includes debug messages
                        }
                        _session.Map[currentX, currentY] = TileType.Explosion; // Turn bomb tile into explosion
                        break;
                    }

                    // If it hits an existing explosion from this tick's chain reaction, stop propagation
                    if (tile == TileType.Explosion)
                    {
                        Console.WriteLine($"  DEBUG: Explosion stopped by existing explosion at ({currentX},{currentY})."); // Note: Includes debug messages
                        break;
                    }

                    // Otherwise, it's an empty tile or powerup, set it to explosion and continue propagation
                    _session.Map[currentX, currentY] = TileType.Explosion;
                    Console.WriteLine($"  DEBUG: Set ({currentX},{currentY}) to Explosion (from Empty/PowerUp)."); // Note: Includes debug messages
                }
            }

            // --- Handle Player Hits ---
            // Check all players against all affected tiles AFTER calculating the full blast area
            Console.WriteLine($"  DEBUG: Checking {affectedTiles.Count} affected tiles for players."); // Note: Includes debug messages
            List<string> playersHitIds = new List<string>();
            foreach (var (x, y) in affectedTiles)
            {
                // Find players at this tile that haven't already been marked as hit by this bomb
                var playersAtTile = _session.Players.Values.Where(p => p.X == x && p.Y == y && !playersHitIds.Contains(p.Id)).ToList();
                foreach (var player in playersAtTile)
                {
                    playersHitIds.Add(player.Id);
                    Console.WriteLine($"  DEBUG: Player {player.Id} hit by explosion at ({x},{y})!"); // Note: Includes debug messages
                }
            }

            // Remove hit players from the game session
            foreach (string playerId in playersHitIds)
            {
                _session.Players.Remove(playerId);
            }
            if (playersHitIds.Count > 0) { Console.WriteLine($"  DEBUG: Removed {playersHitIds.Count} players hit by this explosion."); } // Note: Includes debug messages

        }
    }
}