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
            if (player.X != x || player.Y != y) return false;
            if (player.ActiveBombsCount >= player.MaxBombs) return false;

            _session.AddBomb(playerId, x, y);
            player.ActiveBombsCount++;
            return true;
        }

        // Tick now cleans up old explosions *before* processing new ones
        public void Tick()
        {
            CleanupExplosions();
            UpdateBombs();
        }

        // New method to clear explosion tiles from the previous tick
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
            List<Bomb> bombsToDetonate = new List<Bomb>();
            foreach (var bomb in _session.Bombs.ToList())
            {
                bomb.RemainingFuseTicks--;
                if (bomb.RemainingFuseTicks <= 0)
                {
                    bombsToDetonate.Add(bomb);
                    _session.Bombs.Remove(bomb);
                }
            }

            Queue<Bomb> detonationQueue = new Queue<Bomb>(bombsToDetonate);
            HashSet<Bomb> processedBombs = new HashSet<Bomb>(bombsToDetonate);

            while (detonationQueue.Count > 0)
            {
                var bomb = detonationQueue.Dequeue();
                DetonateBomb(bomb, detonationQueue, processedBombs);
            }
        }

        // DetonateBomb now places Explosion tiles instead of Empty
        private void DetonateBomb(Bomb bomb, Queue<Bomb> detonationQueue, HashSet<Bomb> processedBombs)
        {
            if (_session.Players.TryGetValue(bomb.OwnerId, out Player? owner))
            {
                if (owner.ActiveBombsCount > 0) owner.ActiveBombsCount--;
            }

            HashSet<(int, int)> affectedTiles = new HashSet<(int, int)>();
            affectedTiles.Add((bomb.X, bomb.Y));

            int[] dx = { 0, 0, 1, -1 };
            int[] dy = { 1, -1, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                for (int j = 1; j <= bomb.BlastRadius; j++)
                {
                    int targetX = bomb.X + dx[i] * j;
                    int targetY = bomb.Y + dy[i] * j;

                    if (targetX < 0 || targetX >= _session.Map.GetLength(0) || targetY < 0 || targetY >= _session.Map.GetLength(1))
                        break;

                    affectedTiles.Add((targetX, targetY));
                    TileType tileType = _session.Map[targetX, targetY];

                    if (tileType == TileType.Wall) break; // Stop at indestructible walls
                    if (tileType == TileType.DestructibleWall) break; // Stop *after* hitting destructible walls
                    if (tileType == TileType.Player || tileType == TileType.Bomb) break; // Stop *after* hitting player/bomb
                }
            }

            List<Player> playersToRemove = new List<Player>();
            foreach (var (x, y) in affectedTiles)
            {
                Player? playerOnTile = _session.Players.Values.FirstOrDefault(p => p.X == x && p.Y == y);
                if (playerOnTile != null && !playersToRemove.Contains(playerOnTile))
                {
                    Console.WriteLine($"--- Player {playerOnTile.Id} hit by explosion at ({x},{y})! ---");
                    playersToRemove.Add(playerOnTile);
                }

                Bomb? bombOnTile = _session.Bombs.FirstOrDefault(b => b.X == x && b.Y == y);
                if (bombOnTile != null)
                {
                    if (processedBombs.Add(bombOnTile))
                    {
                        Console.WriteLine($"--- Chain reaction: Bomb at ({x},{y}) triggered! ---");
                        _session.Bombs.Remove(bombOnTile);
                        detonationQueue.Enqueue(bombOnTile);
                    }
                }

                // --- MODIFIED: Update map tile to Explosion ---
                TileType currentTileType = _session.Map[x, y];
                // Turn Empty, Player, Bomb, DestructibleWall into Explosion
                if (currentTileType != TileType.Wall) // Indestructible walls remain
                {
                    _session.Map[x, y] = TileType.Explosion; // Set to explosion
                }
                // --- END MODIFIED ---
            }

            // Remove hit players
            foreach (var playerToRemove in playersToRemove)
            {
                _session.Players.Remove(playerToRemove.Id);
                Console.WriteLine($"--- Player {playerToRemove.Id} removed from game. ---");
            }
        }
    }
}

